using TMPro;
using UnityEngine;

public class SellSelectedEquipmentInfo : MonoBehaviour
{
    // 상점에서 판매할 장비 스탯 인포 확인
    [Header("장비 정보")]
    [SerializeField] private TextMeshProUGUI equipmentInfoText;

    public void ShowEquipment(EquippedItem item)
    {
        if (item == null || item.Data == null)
        {
            equipmentInfoText.text = "장비를 선택해주세요";
            return;
        }

        equipmentInfoText.text =
            item.Data.NameKr + "\n\n" +
            GetStatName(item.Data.Slot) + " +" +
            item.TotalRollPercent.ToString("F1") + "%\n" +
            "강화 +" + item.EnhanceLevel;
    }

    public void Clear()
    {
        equipmentInfoText.text = "장비를 선택해주세요";
    }

    private static string GetStatName(EquipmentSlot slot)
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