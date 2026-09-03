/*
장비 1종의 고정 정보(부위, 착용 제한)를 담는 SO. 구글 시트 Equipment 탭 → CSV 임포터가 채워줌
개별 드랍/장착 시 굴리는 랜덤 % 옵션은 여기 없고 EquippedItem이 따로 들고 있음 (SO는 여러 개체가 공유하는 값이라
이 아이템이 몇 %로 떴는지 같은 개체별 값은 못담음)

부위마다 담당하는 스탯이 고정돼 있음:
  무기=공격력, 갑옷=체력, 바지=골드획득, 장갑=치명타율, 반지=치명타피해, 신발=공격속도
*/

using UnityEngine;

/// <summary>
/// 장비 기본 데이터. 프로젝트 창에서 우클릭 → Create → Game/Equipment Data 로 생성
/// </summary>
[CreateAssetMenu(fileName = "EquipmentData", menuName = "Game/Equipment Data", order = 4)]
public sealed class EquipmentData : ScriptableObject
{
    [Header("식별 정보")]
    [Tooltip("고유 키. 시트: id (예: eq_sword_warrior). 절대 바뀌지 않는값. 세이브/드랍 전달 시 SO 참조 대신 이 id로 식별함")]
    [SerializeField] private string id = "eq_id";

    [Tooltip("표시 이름. 시트: name_kr")]
    [SerializeField] private string nameKr = "이름없음";

    [Header("부위 / 착용 제한")]
    [Tooltip("장비 부위. 부위마다 담당 스탯이 고정됨")]
    [SerializeField] private EquipmentSlot slot = EquipmentSlot.Weapon;

    [Tooltip("무기(Weapon) 부위 전용 - 이 무기를 낄 수 있는 캐릭터의 공격 방식. 무기가 아닌 부위는 무시됨 (전체공통)")]
    [SerializeField] private AttackType allowedAttackType = AttackType.Physical;

    // 고유키 (시트: id)
    public string Id => id;

    // 표시이름
    public string NameKr => nameKr;

    // 장비부위
    public EquipmentSlot Slot => slot;

    // 무기전용 착용제한 (그 외 부위는 의미 없음)
    public AttackType AllowedAttackType => allowedAttackType;
}
