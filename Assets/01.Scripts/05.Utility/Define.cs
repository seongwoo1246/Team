/*
프로젝트 공통 enum 모음.
구글 시트 Enums 탭의 값과 철자가 반드시 똑같아야 한다.
*/

/// <summary>
/// 캐릭터의 공격 방식
/// </summary>
public enum AttackType
{
    // 물리공격
    Physical,
    // 마법공격
    Magic,
    // 힐러
    Heal,
}

/// <summary>
/// 공격 대상 범위
/// </summary>
public enum TargetType
{
    // 한마리만공격
    Single,
    // 다수공격
    Multi,
}

/// <summary>
/// 몬스터 종류
/// </summary>
public enum MonsterKind
{
    Normal,
    Boss,
}

/// <summary>
/// 파티 강화 트랙. 각 트랙은 따로 레벨업되고 모든 캐릭터에 동시 적용된다.
/// (int 값이 UpgradeSystem 내부 배열 인덱스로 쓰이므로 순서 바꾸지 말 것 / 마지막에만 추가)
/// </summary>
public enum UpgradeTrack
{
    // 공격력 (base_power + power_per_level × 이 레벨)
    Power = 0,

    // 체력 (base_hp × hp_growth ^ 이 레벨)
    Hp = 1,

    // 치명타 확률 (crit_chance + crit_chance_per_level × 이 레벨, 100%에서 멈춤)
    Crit = 2,

    // 치명타 피해 배수 (crit_bonus + crit_bonus_per_level × 이 레벨)
    CritDamage = 3,

    // 골드 획득량 배율 (1 + gold_gain_per_level × 이 레벨)
    GoldGain = 4,

    // 공격 속도 (공격 간격을 1 + attack_speed_per_level × 이 레벨 로 나눔)
    AttackSpeed = 5,
}
