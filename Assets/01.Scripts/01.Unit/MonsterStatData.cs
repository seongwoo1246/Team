using UnityEngine;

/// <summary>
/// 몬스터 기본 스탯 데이터. 프로젝트 창에서 우클릭 → Create → Game/Monster Stat Data 로 생성
/// </summary>
[CreateAssetMenu(fileName = "MonsterStat", menuName = "Game/Monster Stat Data", order = 1)]
public class MonsterStatData : ScriptableObject
{
    [Header("식별 정보")]
    [Tooltip("고유 키. 시트: id (예: mon_slime)")]
    [SerializeField] private string id = "mon_id";

    [Tooltip("표시 이름. 시트: name_kr (예: 슬라임)")]
    [SerializeField] private string nameKr = "이름없음";

    [Tooltip("몬스터 종류. 시트: kind (Normal / Boss)")]
    [SerializeField] private MonsterKind kind = MonsterKind.Normal;

    [Header("기본 능력치 (배율 적용 전)")]

    [SerializeField] private float baseHp = 50f;

    [SerializeField] private float baseAttack = 5f;

    [SerializeField] private float baseGold = 10f;

    [SerializeField] private float moveSpeed = 1.5f;

    [Header("레벨(스테이지) 성장률")]
    // 레벨당 체력 증가율
    [SerializeField] private float hpGrowthPerLevel = StatCalculator.DEFAULT_MONSTER_HP_GROWTH;

    // 레벨당 보상 골드 증가율
    [SerializeField] private float goldGrowthPerLevel = StatCalculator.DEFAULT_GOLD_GROWTH;

    public string Id => id;

    public string NameKr => nameKr;

    // 몬스터 종류
    public MonsterKind Kind => kind;

    public float BaseHp => baseHp;

    public float BaseAttack => baseAttack;

    public float BaseGold => baseGold;

    public float MoveSpeed => moveSpeed;

    public float HpGrowthPerLevel => hpGrowthPerLevel;

    public float GoldGrowthPerLevel => goldGrowthPerLevel;
}
