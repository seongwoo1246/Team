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

        currentEquipmentText.text = "현재 장착\n\n" + item.Data.NameKr + "\n" + "옵션 +" 
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
            "선택 장비\n\n" + item.Data.NameKr + "\n" + "옵션 +" 
            + item.TotalRollPercent.ToString("F1") + "%\n" + "강화 +" + item.EnhanceLevel;
    }


    public void Clear()
    {
        currentEquipmentText.text = "현재 장착 없음";
        selectedEquipmentText.text = "선택 장비 없음";
    }
}