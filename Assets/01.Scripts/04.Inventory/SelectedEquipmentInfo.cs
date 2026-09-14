using UnityEngine;
using TMPro;

public class SelectedEquipmentInfo : MonoBehaviour
{
    [Header("현재 장비")]
    [SerializeField] private TextMeshProUGUI currentEquipmentText;

    [Header("선택 장비")]
    [SerializeField] private TextMeshProUGUI selectedEquipmentText;


    public void ShowEquipment(EquippedItem currentItem, EquippedItem selectedItem)
    {
        ShowCurrentEquipment(currentItem);
        ShowSelectedEquipment(selectedItem);
    }


    private void ShowCurrentEquipment(EquippedItem item)
    {
        if (item == null || item.Data == null)
        {
            currentEquipmentText.text = "현재 장착 없음";
            return;
        }

        currentEquipmentText.text = "현재 장착\n\n" + item.Data.NameKr + "\n" + GetStatName(item.Data.Slot) + " +"
            + item.TotalRollPercent.ToString("F1") + "%\n" + "강화 +" + item.EnhanceLevel;
    }


    private void ShowSelectedEquipment(EquippedItem item)
    {
        if (item == null || item.Data == null)
        {
            selectedEquipmentText.text = "선택 장비 없음";
            return;
        }

        selectedEquipmentText.text =
            "선택 장비\n\n" + item.Data.NameKr + "\n" + GetStatName(item.Data.Slot) + " +"
            + item.TotalRollPercent.ToString("F1") + "%\n" + "강화 +" + item.EnhanceLevel;
    }


    /// <summary>
    /// 장비 부위가 담당하는 스탯 이름 (Define.cs의 EquipmentSlot 주석에 적힌 매핑 그대로)
    /// 옵션 텍스트에 "옵션" 대신 실제 스탯 이름을 보여주는 용도
    /// </summary>
    /// <param name="slot">장비 부위</param>
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


    public void Clear()
    {
        currentEquipmentText.text = "현재 장착 없음";
        selectedEquipmentText.text = "선택 장비 없음";
    }
}