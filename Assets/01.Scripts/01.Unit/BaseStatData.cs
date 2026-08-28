/*
캐릭터 1종의 레벨 0 기준값 + 성장 파라미터를 담는 SO
레벨별로 변하는 실제 수치는 저장하지 않고 StatCalculator가 그때그때 계산

CSV 임포터가 컬럼명(snake_case)을 필드명(camelCase)으로 바꿔 채워줌
  id → id,  name_kr → nameKr,  base_power → basePower
*/

using UnityEngine;

/// <summary>
/// 캐릭터 기본 스탯 데이터. 프로젝트 창에서 우클릭 → Create → Game/Character Stat Data 로 생성
/// </summary>
[CreateAssetMenu(fileName = "CharacterStat", menuName = "Game/Character Stat Data", order = 0)]
public class BaseStatData : ScriptableObject
{
    [Header("식별 정보")]
    [Tooltip("고유 키. 시트: id (예: char_warrior). 절대 바뀌지 않는 값")]
    [SerializeField] private string id = "char_id";

    [Tooltip("표시 이름. 시트: name_kr (예: 전사)")]
    [SerializeField] private string nameKr = "이름없음";

    [Header("전투 유형")]
    // 공격방식 시트: attack_type (Physical / Magic / Heal)
    [SerializeField] private AttackType attackType = AttackType.Physical;

    // 공격 대상. 시트: target_type (Single / Multi)
    [SerializeField] private TargetType targetType = TargetType.Single;

    [Header("기본 능력치 (레벨 0 기준)")]
    // 무기 공격력. 힐러는 힐량으로 사용
    [SerializeField] private float basePower = 20f;

    // 레벨당 공격력/힐량 상승치
    [SerializeField] private float powerPerLevel = 5f;

    // 레벨 0 기준 최대 체력. 시트: base_hp
    [SerializeField] private float baseHp = 200f;

    [Header("치명타")]
    // 치명타 확률. 0.15 = 15%. 시트: crit_chance
    [SerializeField] private float critChance = 0.15f;

    // 치명타 시 추가 피해 배수 (0레벨 기준) 1.0 = 평타의 2배, 2.0 = 3배. 시트: crit_bonus
    [SerializeField] private float critBonus = 1.0f;

    // CritDamage 트랙 레벨당 치명타 피해 배수 증가치. 시트: crit_bonus_per_level
    [SerializeField] private float critBonusPerLevel = 0.05f;

    // Crit 트랙 레벨당 치명타 확률 증가치 합계가 1.0(100%)을 넘으면 100%에서 멈춤. 시트: crit_chance_per_level
    [SerializeField] private float critChancePerLevel = 0.01f;

    [Header("성장률")]
    // Hp 트랙 레벨당 체력 증가율 (캐릭터별로 다르게 줄 수 있음). 시트: hp_growth
    [SerializeField] private float hpGrowthRate = StatCalculator.DEFAULT_HP_GROWTH;

    public string Id => id;

    public string NameKr => nameKr;

    /// <summary>공격 방식 (시트: attack_type)</summary>
    public AttackType AttackType => attackType;

    /// <summary>공격 대상 범위 (시트: target_type)</summary>
    public TargetType TargetType => targetType;

    public float BasePower => basePower;

    public float PowerPerLevel => powerPerLevel;

    public float BaseHp => baseHp;

    public float CritChance => critChance;

    public float CritBonus => critBonus;

    public float CritBonusPerLevel => critBonusPerLevel;

    public float CritChancePerLevel => critChancePerLevel;

    public float HpGrowthRate => hpGrowthRate;
}
