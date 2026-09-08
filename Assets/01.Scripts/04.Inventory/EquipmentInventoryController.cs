using UnityEngine;

public class EquipmentInventoryController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject inventoryPanel;

    [Header("Inventory")]
    [SerializeField] private GameObject inventoryGridPanel;
    [SerializeField] private EquipmentInventory equipmentInventory;
    [SerializeField] private GameObject inventorySlotPrefab;

    [Header("Selected Equipment")]
    [SerializeField] private SelectedEquipmentInfo selectedEquipmentInfo;

    [Header("Character Equipment")]
    [SerializeField] private CharacterEquipment characterEquipment;

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

        selectedEquipmentInfo.ShowEquipment(characterEquipment.GetEquippedItem(equipmentSlot),null);

        //확인용 임시 로그, 확인 후 삭제바람
        Debug.Log(equipmentSlot + " 인벤토리 열림");
    }

    private void RefreshInventory(EquipmentSlot slot)
    {
        // 기존 슬롯 삭제
        for (int i = inventoryGridPanel.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(inventoryGridPanel.transform.GetChild(i).gameObject);
        }

        // 해당 부위의 장비 가져오기
        var items = equipmentInventory.GetItemsBySlot(slot);

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

        EquippedItem currentEquipment = characterEquipment.GetEquippedItem(currentSlot);

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

        characterEquipment.EquipItem(currentSlot, selectedEquipment);
        selectedEquipmentInfo.ShowEquipment(selectedEquipment, selectedEquipment);
    }
}