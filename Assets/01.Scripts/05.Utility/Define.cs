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
