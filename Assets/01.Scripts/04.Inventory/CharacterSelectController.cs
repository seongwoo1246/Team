using UnityEngine;

public class CharacterSelectController : MonoBehaviour
{
    //캐릭터 패널 상단 캐릭터 선택 버튼

    private AttackType currentAttackType;
    public AttackType CurrentAttackType => currentAttackType;

    [Header("캐릭터 장비")]
    [SerializeField] private CharacterEquipment characterEquipment;


    // 전사
    public void SelectWarrior()
    {
        currentAttackType = AttackType.Physical;
        characterEquipment.RefreshEquipmentSlots(currentAttackType);
    }

    // 마법사
    public void SelectMage()
    {
        currentAttackType = AttackType.Magic;
        characterEquipment.RefreshEquipmentSlots(currentAttackType);
    }

    // 힐러
    public void SelectHealer()
    {
        currentAttackType = AttackType.Heal;
        characterEquipment.RefreshEquipmentSlots(currentAttackType);

    }
}