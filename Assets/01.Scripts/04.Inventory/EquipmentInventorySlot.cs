using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentInventorySlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;

    private EquippedItem equippedItem;
    private EquipmentInventoryController controller;

    public void SetItem(EquippedItem item, EquipmentInventoryController inventoryController)
    {
        equippedItem = item;
        controller = inventoryController;

        if (equippedItem == null || equippedItem.Data == null)
            return;

        nameText.text = equippedItem.Data.NameKr;

        Button button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnSlotClicked);
        }
    }

    // 장착된 장비를 표시할 때 사용
    public void SetEquippedItem(EquippedItem item)
    {
        equippedItem = item;

        if (equippedItem == null || equippedItem.Data == null)
        {
            nameText.text = "";
            return;
        }

        nameText.text = equippedItem.Data.NameKr;
        Button button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
    }

    private void OnSlotClicked()
    {
        if (equippedItem == null)
            return;

        controller.SelectEquipment(equippedItem);
    }

    public EquippedItem GetItem()
    {
        return equippedItem;
    }
}