using System.Collections.Generic;
using UnityEngine;

public class ShopSellController : MonoBehaviour
{
    [Header("판매 패널")]
    [SerializeField] private GameObject sellPanel;

    [Header("판매 장비 목록")]
    [SerializeField] private GameObject sellInventoryGridPanel;

    [Header("장비 인벤토리")]
    [SerializeField] private EquipmentInventory equipmentInventory;
    [SerializeField] private GameObject inventorySlotPrefab;

    [Header("판매 설정")]
    [SerializeField] private int goldPerItem = 10;

    // 현재 선택된 장비 목록
    private readonly List<EquippedItem> selectedItems =
        new List<EquippedItem>();

    // 장비와 화면 슬롯을 연결하기 위한 Dictionary
    private readonly Dictionary<EquippedItem, EquipmentInventorySlot> slotMap =
        new Dictionary<EquippedItem, EquipmentInventorySlot>();


    // 판매 창 열기
    public void OpenSellPanel()
    {
        sellPanel.SetActive(true);
        RefreshSellInventory();
    }


    // 판매 창 닫기
    public void CloseSellPanel()
    {
        selectedItems.Clear();
        slotMap.Clear();

        sellPanel.SetActive(false);
    }


    // 판매 목록 새로고침
    public void RefreshSellInventory()
    {
        selectedItems.Clear();
        slotMap.Clear();

        // 기존에 생성된 판매 슬롯 삭제
        for (int i = sellInventoryGridPanel.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(sellInventoryGridPanel.transform.GetChild(i).gameObject);
        }

        // 장비 인벤토리에 있는 모든 장비를 판매 목록에 생성
        foreach (EquippedItem item in equipmentInventory.Items)
        {
            if (item == null || item.Data == null)
                continue;

            GameObject slotObject = Instantiate(
                inventorySlotPrefab,
                sellInventoryGridPanel.transform
            );

            EquipmentInventorySlot slot =
                slotObject.GetComponent<EquipmentInventorySlot>();

            if (slot == null)
            {
                Debug.LogError("InventorySlotPrefab에 EquipmentInventorySlot이 없습니다.");
                continue;
            }

            // 판매용 클릭 함수 연결
            slot.SetSellItem(item, ToggleSelectedItem);

            // 장비와 슬롯 연결
            slotMap[item] = slot;
        }
    }


    // 장비 선택 또는 선택 해제
    private void ToggleSelectedItem(EquippedItem item)
    {
        if (item == null)
            return;

        // 이미 선택된 장비라면 선택 해제
        if (selectedItems.Contains(item))
        {
            selectedItems.Remove(item);

            if (slotMap.TryGetValue(item, out EquipmentInventorySlot slot))
            {
                slot.SetSelected(false);
            }
        }
        else
        {
            // 선택되지 않은 장비라면 선택
            selectedItems.Add(item);

            if (slotMap.TryGetValue(item, out EquipmentInventorySlot slot))
            {
                slot.SetSelected(true);
            }
        }
    }


    // 선택된 장비 판매
    public void SellSelectedItems()
    {
        if (selectedItems.Count == 0)
        {
            Debug.Log("판매할 장비를 선택해주세요.");
            return;
        }

        int sellCount = 0;

        // 선택된 장비를 인벤토리에서 제거
        foreach (EquippedItem item in selectedItems)
        {
            if (equipmentInventory.RemoveItem(item))
            {
                sellCount++;
            }
        }

        // 판매한 개수만큼 골드 지급
        if (sellCount > 0)
        {
            int totalGold = sellCount * goldPerItem;

            GoldWallet.Instance.Add(totalGold);

            Debug.Log(
                sellCount + "개의 장비를 판매했습니다. 획득 골드: " + totalGold
            );
        }

        // 선택 목록 초기화 후 판매 목록 새로고침
        selectedItems.Clear();
        RefreshSellInventory();
    }
}