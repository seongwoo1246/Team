/*
드랍되거나 장착된 장비 개별 인스턴스. MonoBehaviour도 SO도 아닌 순수 C# 클래스
EquipmentData(SO)는 여러 개체가 공유하는 고정 정보고, 실제로 몇% 옵션으로 떴는지는 개체마다 달라서
그 값(rollPercent)만 따로 들고 다님

인벤토리 시스템에서 세이브/전달할 땐 이 클래스를 통째로 쓰지 말고
Data.Id(문자열) + RollPercent + EnhanceRolls 정도만 저장했다가, 불러올 때 id로 EquipmentData를 다시 찾아 재구성할 것

장비 강화(+10까지)도 이 인스턴스 단위로 적용됨. 강화 1회 = 1~3% 랜덤 보너스가 하나 더 쌓이는 것뿐이라
드랍될 때 뜬 원래 rollPercent랑 계산 방식이 똑같음(그냥 최종 합산에 더 들어감) - TotalRollPercent가 이 둘을 합쳐서 돌려줌
*/

using System;
using System.Collections.Generic;
using UnityEngine;

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

    [Tooltip("이 인스턴스가 어떤 장비인지 (고정 정보)")]
    [SerializeField] private EquipmentData data;

    [Tooltip("드랍될 때 굴린 랜덤 보너스 (1~10 사이, %). Data.Slot이 담당하는 스탯에 이 값만큼 % 로 적용됨")]
    [SerializeField] private float rollPercent;

    [Tooltip("강화로 쌓인 보너스% 목록. 강화 1회당 하나씩 추가됨 (최대 10개 = +10)")]
    [SerializeField] private List<float> enhanceRolls = new List<float>();

    /// <summary>
    /// 장비 인스턴스를 만든다. 보통 몬스터 드랍 시 랜덤 롤로 생성함 (강화 0회 상태로 시작)
    /// </summary>
    /// <param name="data">어떤 장비인지 (고정 정보)</param>
    /// <param name="rollPercent">이번에 뜬 랜덤 보너스 (1~10 사이, %)</param>
    public EquippedItem(EquipmentData data, float rollPercent)
    {
        this.data = data;
        this.rollPercent = rollPercent;
    }

    // 어떤 장비인지 (고정 정보)
    public EquipmentData Data => data;

    // 드랍될 때 굴린 랜덤 보너스 (강화분 제외, 1~10 사이 %)
    public float RollPercent => rollPercent;

    // 현재 강화 단계 (+0 ~ +10)
    public int EnhanceLevel => enhanceRolls.Count;

    // 더 강화할 수 있는지 (+10 미만이어야 함)
    public bool CanEnhance => EnhanceLevel < MAX_ENHANCE_LEVEL;

    // 원래 드랍 보너스% + 강화로 쌓인 보너스% 전부 합친 최종 값. 스탯 계산은 전부 이 값을 씀
    public float TotalRollPercent
    {
        get
        {
            float total = rollPercent;
            for (int i = 0; i < enhanceRolls.Count; i++)
            {
                total += enhanceRolls[i];
            }
            return total;
        }
    }

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
        enhanceRolls.Add(addedRollPercent);
        return true;
    }
}
