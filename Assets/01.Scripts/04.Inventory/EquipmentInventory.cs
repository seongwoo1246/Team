using System.Collections.Generic;
using UnityEngine;

public class EquipmentInventory : MonoBehaviour
{
    private List<EquippedItem> items = new List<EquippedItem>();

    public List<EquippedItem> Items => items;



    private void Start()
    {
        
    }


    // 장비 추가
    public void AddItem(EquippedItem item)
    {
        if (item == null)
            return;

        items.Add(item);
    }

    // 장비를 인벤토리에서 제거
    public void RemoveItem(EquippedItem item)
    {
        if (item == null)
            return;

        items.Remove(item);
    }

    // 장비를 종류 별로 불러옴
    public List<EquippedItem> GetItemsBySlot(EquipmentSlot slot)
    {
        List<EquippedItem> result = new List<EquippedItem>();

        for (int i = 0; i < items.Count; i++)
        {
            EquippedItem item = items[i];

            if (item != null && item.Data != null && item.Data.Slot == slot)
            {
                result.Add(item);
            }
        }

        return result;
    }
}