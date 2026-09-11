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

    [Header("캐릭터 선택")]
    [SerializeField] private CharacterSelectController characterSelectController;

    [Header("장비 변경 확인창")]
    [SerializeField] private GameObject confirmPanel;

    [Header("장착 장비 슬롯")]
    [SerializeField] private EquipmentInventorySlot weaponSlot;
    [SerializeField] private EquipmentInventorySlot armorSlot;
    [SerializeField] private EquipmentInventorySlot pantsSlot;
    [SerializeField] private EquipmentInventorySlot glovesSlot;
    [SerializeField] private EquipmentInventorySlot ringSlot;
    [SerializeField] private EquipmentInventorySlot shoesSlot;

    private EquippedItem selectedEquipment;
    private EquipmentSlot currentSlot;

    private void OnEnable()
    {
        RefreshEquippedSlots();
    }

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
        selectedEquipmentInfo.ShowEquipment(characterSelectController.CurrentCharacter.GetEquipped(equipmentSlot), null);
    }

    public void RefreshEquippedSlots()
    {
        CharacterBase character = characterSelectController.CurrentCharacter;

        if (character == null)
            return;

        weaponSlot.SetEquippedItem(character.GetEquipped(EquipmentSlot.Weapon));
        armorSlot.SetEquippedItem(character.GetEquipped(EquipmentSlot.Armor));
        pantsSlot.SetEquippedItem(character.GetEquipped(EquipmentSlot.Pants));
        glovesSlot.SetEquippedItem(character.GetEquipped(EquipmentSlot.Gloves));
        ringSlot.SetEquippedItem(character.GetEquipped(EquipmentSlot.Ring));
        shoesSlot.SetEquippedItem(character.GetEquipped(EquipmentSlot.Shoes));
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
        EquippedItem currentEquipment = characterSelectController.CurrentCharacter.GetEquipped(currentSlot);
        selectedEquipmentInfo.ShowEquipment(currentEquipment, selectedEquipment);
    }

    public void CloseInventory()
    {
        inventoryPanel.SetActive(false);
        statsPanel.SetActive(true);
    }

    // 장비 장착(장비 교체) 버튼 
    public void EquipSelectedEquipment()
    {
        if (selectedEquipment == null)
            return;

        confirmPanel.SetActive(true);

    }

    // 장비변경 확정창 Yes 버튼 (장비가 변경됌)
    public void ConfirmEquipmentChange()
    {
        if (selectedEquipment == null)
            return;

        characterSelectController.CurrentCharacter.Equip(currentSlot, selectedEquipment);
        selectedEquipmentInfo.ShowEquipment(selectedEquipment, selectedEquipment);
        confirmPanel.SetActive(false);

    }

    // 장비변경 확정창 NO 버튼 (그냥 변경 확정창만 닫힘)
    public void CancelEquipmentChange()
    {
        confirmPanel.SetActive(false);
    }
}