using UnityEngine;

public class CharacterSelectController : MonoBehaviour
{
    [Header("캐릭터")]
    [SerializeField] private CharacterBase warrior;
    [SerializeField] private CharacterBase mage;
    [SerializeField] private CharacterBase healer;

    [Header("장비 UI")]
    [SerializeField] private EquipmentInventoryController equipmentInventoryController;

    [Header("스탯 UI")]
    [SerializeField] private CharacterStatsPanel characterStatsPanel;

    private AttackType currentAttackType = AttackType.Physical;

    public AttackType CurrentAttackType => currentAttackType;


    // 현재 선택된 캐릭터
    public CharacterBase CurrentCharacter
    {
        get
        {
            switch (currentAttackType)
            {
                case AttackType.Physical:
                    return warrior;

                case AttackType.Magic:
                    return mage;

                case AttackType.Heal:
                    return healer;
            }

            return null;
        }
    }

    // 캐릭터 패널 상단 캐릭터 선택버튼
    public void SelectWarrior()
    {
        currentAttackType = AttackType.Physical;
        equipmentInventoryController.RefreshEquippedSlots();
        characterStatsPanel.RefreshStats();
    }

    public void SelectMage()
    {
        currentAttackType = AttackType.Magic;
        equipmentInventoryController.RefreshEquippedSlots();
        characterStatsPanel.RefreshStats();
    }

    public void SelectHealer()
    {
        currentAttackType = AttackType.Heal;
        equipmentInventoryController.RefreshEquippedSlots();
        characterStatsPanel.RefreshStats();
    }
}