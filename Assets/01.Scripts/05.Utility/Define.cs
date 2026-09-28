// 작성자: 김주연
/*
프로젝트 공통 enum 모음.
구글 시트 Enums 탭의 값과 철자가 반드시 똑같아야 한다.
*/

/// <summary>
/// 캐릭터의 공격 방식
/// 원래는 물리/마법/힐 셋뿐이었지만, 팔라딘/궁수 추가하면서 각자 전용 값을 새로 받음
/// (장비 인벤토리가 "캐릭터 한 명 = AttackType 하나"로 구분하는 구조라, 팔라딘/궁수도 Physical을
/// 같이 쓰면 전사랑 장비 칸을 공유해버림 - 그래서 새 값을 받는 쪽을 택함. 대신 무기는
/// AttackType이 일치해야만 착용 가능이라, 팔라딘/궁수 전용 무기를 새로 만들어야 함.
/// 갑옷/바지/장갑/반지/신발은 AttackType 제한이 없어서 기존 것 그대로 같이 씀)
/// </summary>
public enum AttackType
{
    // 물리공격 (전사)
    Physical,
    // 마법공격
    Magic,
    // 힐러
    Heal,
    // 팔라딘 전용
    Paladin,
    // 궁수 전용
    Archer,
}

/// <summary>
/// 캐릭터의 전열/후열 위치. 몬스터 AI가 타겟 우선순위를 정할 때 씀 (예: 후열 우선 타겟팅)
/// </summary>
public enum CharacterRow
{
    // 전열 (예: 전사 - 앞에서 버티는 역할)
    Front,
    // 후열 (예: 마법사, 힐러 - 뒤에서 지원하는 역할)
    Back,
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
/// 파티 강화 트랙. 각 트랙은 따로 레벨업되고 모든 캐릭터에 동시 적용
/// (int 값이 UpgradeSystem 내부 배열 인덱스로 쓰이므로 순서 바꾸지 말것 / 마지막에만 추가)
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

/// <summary>
/// 스테이지 진행 모드
/// </summary>
public enum StageMode
{
    // 메인 화면 무한 파밍 (몬스터 체력 관리 없이 계속 사냥)
    Farming,

    // 챌린지 스테이지 (지정된 웨이브를 순서대로 진행한 뒤 보스)
    Challenge,
}

/// <summary>
/// 장비 부위. 부위마다 담당하는 스탯이 고정되있다
/// (Weapon=공격력, Armor=체력, Pants=골드획득, Gloves=치명타율, Ring=치명타피해, Shoes=공격속도)
/// 무기(Weapon)만 캐릭터의 AttackType에 맞는 것만 장착 가능하고, 나머지 5부위는 캐릭터 공통
/// </summary>
public enum EquipmentSlot
{
    // 무기 - 공격력. 캐릭터마다 전용 무기가 따로 있음 (AttackType으로 제한)
    Weapon,

    // 갑옷 - 체력
    Armor,

    // 바지 - 골드 획득량 (파티 전체에 적용, 캐릭터 개인스탯 아님주의)
    Pants,

    // 장갑 - 치명타율
    Gloves,

    // 반지 - 치명타 피해
    Ring,

    // 신발 - 공격 속도
    Shoes,
}

/// <summary>
/// EquipmentSlot 관련 공용 헬퍼. SelectedEquipmentInfo.cs/SellSelectedEquipmentInfo.cs에
/// 완전히 똑같은 GetStatName switch문이 복붙돼있어서 여기 하나로 합침
/// </summary>
public static class EquipmentSlotHelper
{
    public static string GetStatName(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon:
                return "공격력";
            case EquipmentSlot.Armor:
                return "체력";
            case EquipmentSlot.Pants:
                return "골드획득";
            case EquipmentSlot.Gloves:
                return "치명타율";
            case EquipmentSlot.Ring:
                return "치명타피해";
            case EquipmentSlot.Shoes:
                return "공격속도";
            default:
                return "옵션";
        }
    }
}

// 작성자: 김주연
/// <summary>
/// 장비 등급. SO(EquipmentData)에 고정된 값이 아니라 드랍될 때(EquippedItem.CreateFromDrop) 랜덤으로 정해짐
/// (하급/중급/상급이 각자 다른 % 보너스 범위를 씀 - EquippedItem.cs의 등급별 ROLL 범위 상수 참고)
/// </summary>
public enum EquipmentGrade
{
    // 하급 - 기존 드랍 범위(1~10%) 그대로
    Low,

    // 중급
    Mid,

    // 상급
    High,
}

/// <summary>
/// EquipmentGrade 관련 공용 헬퍼 (표시 이름)
/// </summary>
public static class EquipmentGradeHelper
{
    public static string GetDisplayName(EquipmentGrade grade)
    {
        switch (grade)
        {
            case EquipmentGrade.Low:
                return "하급";
            case EquipmentGrade.Mid:
                return "중급";
            case EquipmentGrade.High:
                return "상급";
            default:
                return "-";
        }
    }
}
