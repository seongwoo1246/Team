using TMPro;
using UnityEngine;

public class CharacterStatsPanel : MonoBehaviour
{
    [Header("스탯 텍스트")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI atkText;
    [SerializeField] private TextMeshProUGUI attackSpeedText;
    [SerializeField] private TextMeshProUGUI goldGainText;
    [SerializeField] private TextMeshProUGUI critChanceText;
    [SerializeField] private TextMeshProUGUI critDamageText;

    [Header("캐릭터 선택")]
    [SerializeField] private CharacterSelectController characterSelectController;

    private void OnEnable()
    {
        RefreshStats();
    }

    public void RefreshStats()
    {
        if (characterSelectController == null)
            return;

        CharacterBase character = characterSelectController.CurrentCharacter;

        if (character == null)
            return;
    
        // 실제 캐릭터의 현재 HP, 공격력
        hpText.text = "HP: " + character.MaxHP.ToString("F0");
        atkText.text = "ATK: " + character.Power.ToString("F1");

        // 현재 장착 장비의 부위별 보너스
        // 스탯창에서 10단위로 보여주려고
        float attackSpeedBonus = character.GetEquippedBonusRatio(EquipmentSlot.Shoes) * 100f;
        float goldGainBonus = character.GetEquippedBonusRatio(EquipmentSlot.Pants) * 100f;
        float critChanceBonus = character.GetEquippedBonusRatio(EquipmentSlot.Gloves) * 100f;
        float critDamageBonus = character.GetEquippedBonusRatio(EquipmentSlot.Ring) * 100f;

        attackSpeedText.text = "A.S: " + attackSpeedBonus.ToString("F1") + "%";
        goldGainText.text = "G.G: " + goldGainBonus.ToString("F1") + "%";
        critChanceText.text = "C.C: " + critChanceBonus.ToString("F1") + "%";
        critDamageText.text = "C.D: " + critDamageBonus.ToString("F1") + "%";
    }
}