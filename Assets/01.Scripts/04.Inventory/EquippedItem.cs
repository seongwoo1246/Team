// 작성자: 김주연
/*
드랍되거나 장착된 장비 개별 인스턴스. MonoBehaviour도 SO도 아닌 순수 C# 클래스
EquipmentData(SO)는 여러 개체가 공유하는 고정 정보고, 실제로 몇% 옵션으로 떴는지는 개체마다 달라서
그 값(rollPercent)만 따로 들고 다님

인벤토리 시스템에서 세이브/전달할 땐 이 클래스를 통째로 쓰지 말고
Data.Id(문자열) + RollPercent + EnhanceLevel + EnhanceBonusTotal 정도만 저장했다가, 불러올 때 id로 EquipmentData를 다시 찾아 재구성할 것

장비 강화(+10까지)도 이 인스턴스 단위로 적용됨. 강화 1회 = 1~3% 랜덤 보너스가 하나 더 쌓이는 것뿐이라
드랍될 때 뜬 원래 rollPercent랑 계산 방식이 똑같음(그냥 최종 합산에 더 들어감) - TotalRollPercent가 이 둘을 합쳐서 돌려줌

강화 이력은 몇 번째 강화 때 몇 %가 떴는지 개별로 쓰는 곳이 없어서, 리스트로 안 쌓고
enhanceLevel(몇 강인지) + enhanceBonusTotal(누적 % 합) 두값으로만 관리함
(세이브이슈)
*/
/* 공동 작성사 - 송태훈
 
 */


using System;
using UnityEngine;

public enum DropType
{
    Gold, Material, Equipment
}
public struct DropReward
{
    public DropType Type;
    public int Amount;
    public EquipmentData EquipData;
}



/// <summary>
/// 장비 1개의 실제 인스턴스 (어떤 EquipmentData인지 + 몇 %로 떴는지 + 강화로 쌓인 보너스)
/// </summary>
[Serializable]
public sealed class EquippedItem
{
    // 강화 가능한 최대 횟수 (+10)
    private const int MAX_ENHANCE_LEVEL = 10;

    // 강화 1회당 랜덤으로 붙는 보너스 범위 (%)
    private const float ENHANCE_ROLL_MIN = 1f;
    private const float ENHANCE_ROLL_MAX = 3f;

    public const float DROP_ROLL_MIN = 1f;
    public const float DROP_ROLL_MAX = 10f;

    #region 김주연 - 장비 등급(중급/상급) 추가
    // 등급별 랜덤 보너스 범위(%). 하급은 기존 DROP_ROLL_MIN~MAX 그대로 씀
    private const float MID_ROLL_MIN = 10f;
    private const float MID_ROLL_MAX = 25f;
    private const float HIGH_ROLL_MIN = 25f;
    private const float HIGH_ROLL_MAX = 50f;

    // 몬스터 드랍 등급 가중치 (하급/중급/상급 순. 합계가 100일 필요는 없고 비율만 맞으면 됨)
    private const float DROP_GRADE_WEIGHT_LOW = 70f;
    private const float DROP_GRADE_WEIGHT_MID = 25f;
    private const float DROP_GRADE_WEIGHT_HIGH = 5f;

    // 가챠 등급 가중치 (하급 없이 중급/상급만 나오게)
    private const float GACHA_GRADE_WEIGHT_LOW = 0f;
    private const float GACHA_GRADE_WEIGHT_MID = 90f;
    private const float GACHA_GRADE_WEIGHT_HIGH = 10f;

    /// <summary>가중치대로 등급 하나를 랜덤으로 뽑는다</summary>
    private static EquipmentGrade RollGrade(float weightLow, float weightMid, float weightHigh)
    {
        float totalWeight = weightLow + weightMid + weightHigh;
        float roll = UnityEngine.Random.Range(0f, totalWeight);

        if (roll < weightLow)
        {
            return EquipmentGrade.Low;
        }

        if (roll < weightLow + weightMid)
        {
            return EquipmentGrade.Mid;
        }

        return EquipmentGrade.High;
    }

    /// <summary>등급에 맞는 랜덤 보너스% 범위를 돌려준다</summary>
    private static float RollPercentForGrade(EquipmentGrade grade)
    {
        switch (grade)
        {
            case EquipmentGrade.Mid:
                return UnityEngine.Random.Range(MID_ROLL_MIN, MID_ROLL_MAX);
            case EquipmentGrade.High:
                return UnityEngine.Random.Range(HIGH_ROLL_MIN, HIGH_ROLL_MAX);
            default:
                return UnityEngine.Random.Range(DROP_ROLL_MIN, DROP_ROLL_MAX);
        }
    }
    #endregion


    [Tooltip("서버 인벤토리 고유 식별자(GUID)")] // 송태훈
    [SerializeField] private string instanceId;

    [Tooltip("이 인스턴스가 어떤 장비인지 (고정 정보)")]
    [SerializeField] private EquipmentData data;

    [Tooltip("드랍될 때 굴린 랜덤 보너스 (등급별 범위 다름, %). Data.Slot이 담당하는 스탯에 이 값만큼 % 로 적용됨")]
    [SerializeField] private float rollPercent;

    [Tooltip("장비 등급 (하급/중급/상급). 드랍 시 가중치대로 랜덤 결정")]
    [SerializeField] private EquipmentGrade grade;

    [Tooltip("현재 강화 단계 (+0 ~ +10)")]
    [SerializeField] private int enhanceLevel;

    [Tooltip("강화로 쌓인 보너스% 합계. 강화 1회마다 이번에 뜬 % 만큼 누적됨")]
    [SerializeField] private float enhanceBonusTotal;

    /// <summary>
    /// 장비 인스턴스를 만든다. 보통 몬스터 드랍 시 랜덤 롤로 생성함 (강화 0회 상태로 시작)
    /// </summary>
    /// <param name="data">어떤 장비인지 (고정 정보)</param>
    /// <param name="rollPercent">이번에 뜬 랜덤 보너스 (등급별 범위, %)</param>
    /// <param name="grade">이번에 뜬 등급 (하급/중급/상급)</param>
    public EquippedItem(EquipmentData data, float rollPercent, EquipmentGrade grade = EquipmentGrade.Low)
    {
        this.instanceId = Guid.NewGuid().ToString();
        this.data = data;
        this.rollPercent = rollPercent;
        this.grade = grade;
    }

    /// <summary>
    /// 드랍 공용 생성 함수. 등급(하급/중급/상급)을 가중치대로 먼저 뽑고, 그 등급 범위 안에서
    /// 랜덤 보너스를 굴려 새 장비 인스턴스를 만든다.
    /// 몬스터 드랍, 가챠등 새장비를 드랍시키는 모든 곳에서 이 함수 하나만 쓰면 됨
    /// </summary>
    /// <param name="data">어떤 장비인지 (고정 정보)</param>
    public static EquippedItem CreateFromDrop(EquipmentData data)
    {
        EquipmentGrade grade = RollGrade(DROP_GRADE_WEIGHT_LOW, DROP_GRADE_WEIGHT_MID, DROP_GRADE_WEIGHT_HIGH);
        float rollPercent = RollPercentForGrade(grade);
        return new EquippedItem(data, rollPercent, grade);
    }

    /// <summary>
    /// 가챠 전용 생성 함수. 하급 없이 중급/상급 가중치로만 등급을 뽑는다
    /// </summary>
    /// <param name="data">어떤 장비인지 (고정 정보)</param>
    public static EquippedItem CreateFromGacha(EquipmentData data)
    {
        EquipmentGrade grade = RollGrade(GACHA_GRADE_WEIGHT_LOW, GACHA_GRADE_WEIGHT_MID, GACHA_GRADE_WEIGHT_HIGH);
        float rollPercent = RollPercentForGrade(grade);
        return new EquippedItem(data, rollPercent, grade);
    }

    /// <summary>
    /// 서버 EquipmentSaveDTO 복원용 생성자
    /// </summary>
    public EquippedItem(string instanceId, EquipmentData data, EquipmentSaveDTO dto)
    {
        this.instanceId = instanceId;
        this.data = data;
        this.rollPercent = dto.rollPercent;
        this.grade = dto.grade;
        this.enhanceLevel = dto.enhanceLevel;
        this.enhanceBonusTotal = dto.totalEnhanceBonus;
    }

    // 서버 고유 식별자 GUID
    public string InstanceId => instanceId;
    // 어떤 장비인지 (고정 정보)
    public EquipmentData Data => data;
    // 장비 등급 (하급/중급/상급)
    public EquipmentGrade Grade => grade;

    // 드랍될 때 굴린 랜덤 보너스 (강화분 제외, 1~10 사이 %)
    public float RollPercent => rollPercent;

    // 현재 강화 단계 (+0 ~ +10)
    public int EnhanceLevel => enhanceLevel;

    // 강화로 쌓인 보너스% 합계 (드랍 시 rollPercent는 제외, 서버 저장 호출에 씀)
    public float EnhanceBonusTotal => enhanceBonusTotal;

    // 더 강화할 수 있는지 (+10 미만이어야 함)
    public bool CanEnhance => EnhanceLevel < MAX_ENHANCE_LEVEL;

    // 원래 드랍 보너스% + 강화로 쌓인 보너스% 전부 합친 최종 값. 스탯 계산은 전부 이값을씀
    public float TotalRollPercent => rollPercent + enhanceBonusTotal;

    // 이 장비를 현재 장착하고 있는 캐릭터
    public CharacterBase EquippedBy { get; private set; }

    // 장비가 현재 장착 중인지 확인
    public bool IsEquipped => EquippedBy != null;

    /// <summary>
    /// 이 장비를 1강 강화한다. 재료 소모/성공 여부 판정은 호출하는 쪽(CharacterBase.TryEnhanceEquipped)이
    /// 담당하고, 여기서는 이미 강화하기로 확정된 순간의 랜덤 보너스 굴림 + 누적만 처리함
    /// </summary>
    /// <param name="addedRollPercent">이번 강화로 새로 붙은 보너스% (실패 시 0)</param>
    /// <returns>강화 성공 여부 (이미 +10이면 false)</returns>
    public bool TryEnhance(out float addedRollPercent)
    {
        addedRollPercent = 0f;

        if (!CanEnhance)
        {
            return false;
        }

        addedRollPercent = UnityEngine.Random.Range(ENHANCE_ROLL_MIN, ENHANCE_ROLL_MAX);
        enhanceLevel++;
        enhanceBonusTotal += addedRollPercent;
        return true;
    }

    // 장비를 장착한 캐릭터 정보 설정 또는 해제
    public void SetEquippedBy(CharacterBase character)
    {
        EquippedBy = character;
    }
    #region 추가 작업물 - 송태훈
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public EquipmentSaveDTO ToDTO() => new EquipmentSaveDTO
    {
        dataId = data != null ? data.Id : string.Empty,
        rollPercent = rollPercent,
        grade = grade,
        enhanceLevel = enhanceLevel,
        totalEnhanceBonus = enhanceBonusTotal,
    };
    #endregion
}
