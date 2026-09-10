using UnityEngine;
using System.Collections.Generic;

public class EquipmentInventoryController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject inventoryPanel;

    [Header("Inventory")]
    [SerializeField] private GameObject inventoryGridPanel;
    [SerializeField] private EquipmentInventory equipmentInventory;
    [SerializeField] private GameObject inventorySlotPrefab;

    [Header("장비 정보 비교 패널")]
    [SerializeField] private SelectedEquipmentInfo selectedEquipmentInfo;

    [Header("캐릭터 장비")]
    [SerializeField] private CharacterEquipment characterEquipment;

    [Header("캐릭터 선택")]
    [SerializeField] private CharacterSelectController characterSelectController;

    [Header("장비 변경 확인창")]
    [SerializeField] private GameObject confirmPanel;

    private EquippedItem selectedEquipment;
    private EquipmentSlot currentSlot;

    public void OpenWeapon()
    {
        OpenInventory(EquipmentSlot.Weapon);
    }

    public void OpenArmor()
    {
        OpenInventory(EquipmentSlot.Armor);
    }

    public void OpenPants()
    {
        OpenInventory(EquipmentSlot.Pants);
    }

    public void OpenGloves()
    {
        OpenInventory(EquipmentSlot.Gloves);
    }

    public void OpenShoes()
    {
        OpenInventory(EquipmentSlot.Shoes);
    }

    public void OpenRing()
    {
        OpenInventory(EquipmentSlot.Ring);
    }

    private void OpenInventory(EquipmentSlot equipmentSlot)
    {
        // 장비 종류 별로 인벤토리 열기
        currentSlot = equipmentSlot;

        statsPanel.SetActive(false);
        inventoryPanel.SetActive(true);
        RefreshInventory(equipmentSlot);
        selectedEquipmentInfo.ShowEquipment(characterEquipment.GetEquippedItem(characterSelectController.CurrentAttackType,equipmentSlot),null);
    }

    private void RefreshInventory(EquipmentSlot slot)
    {
        // 기존 슬롯 삭제
        for (int i = inventoryGridPanel.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(inventoryGridPanel.transform.GetChild(i).gameObject);
        }

        // 장비를 캐릭터에 따라 다르게 가져옴
        List<EquippedItem> items;

        if (slot == EquipmentSlot.Weapon)
        {
            AttackType attackType = characterSelectController.CurrentAttackType;
            items = equipmentInventory.GetItemsBySlotAndAttackType(slot, attackType);
        }
        else
        {
            items = equipmentInventory.GetItemsBySlot(slot);
        }

        // 장비 슬롯 생성
        for (int i = 0; i < items.Count; i++)
        {
            GameObject slotObject = Instantiate(inventorySlotPrefab, inventoryGridPanel.transform);
            EquipmentInventorySlot inventorySlot = slotObject.GetComponent<EquipmentInventorySlot>();
            inventorySlot.SetItem(items[i], this);
        }
    }

    // Grid에서 장비를 클릭했을 때 호출
    public void SelectEquipment(EquippedItem item)
    {
        if (item == null)
            return;

        selectedEquipment = item;
        EquippedItem currentEquipment = characterEquipment.GetEquippedItem(characterSelectController.CurrentAttackType,currentSlot);
        selectedEquipmentInfo.ShowEquipment(currentEquipment, selectedEquipment);
    }

    public void CloseInventory()
    {
        inventoryPanel.SetActive(false);
        statsPanel.SetActive(true);

        // 인벤토리창 닫을때 직업별 장비 저장용
        characterEquipment.RefreshEquipmentSlots(characterSelectController.CurrentAttackType);

        // 장비 변경 확정창 띄우기
        //confirmPanel.SetActive(true);
    }

    // 장비 장착(장비 교체) 버튼 
    public void EquipSelectedEquipment()
    {
        if (selectedEquipment == null)
            return;

        //characterEquipment.EquipItem(characterSelectController.CurrentAttackType, currentSlot, selectedEquipment);
        //selectedEquipmentInfo.ShowEquipment(selectedEquipment, selectedEquipment);

        confirmPanel.SetActive(true);

    }

    // 장비변경 확정창 Yes 버튼 (장비가 변경됌)
    public void ConfirmEquipmentChange()
    {
        if (selectedEquipment == null)
            return;

        characterEquipment.EquipItem(characterSelectController.CurrentAttackType, currentSlot, selectedEquipment);
        selectedEquipmentInfo.ShowEquipment(selectedEquipment, selectedEquipment);
        confirmPanel.SetActive(false);

    }

    // 장비변경 확정창 NO 버튼 (그냥 변경 확정창만 닫힘)
    public void CancelEquipmentChange()
    {
        confirmPanel.SetActive(false);
    }
}