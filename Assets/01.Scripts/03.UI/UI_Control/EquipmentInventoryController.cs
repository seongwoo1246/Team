/*
담담자 - 홍준호
 캐릭터 패널 장비 장착 및 인벤토리 총괄 스크립트
 */
using System.Collections.Generic;
using TMPro;
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

    [Header("장비 정보 비교 패널")]
    [SerializeField] private SelectedEquipmentInfo selectedEquipmentInfo;

    [Header("캐릭터 선택")]
    [SerializeField] private CharacterSelectController characterSelectController;

    [Header("장비 변경 확인창")]
    [SerializeField] private GameObject confirmPanel;

    [Header("장비 중복 장착 안내창")]
    [SerializeField] private GameObject alreadyEquippedPanel;

    [Header("장착 장비 슬롯")]
    [SerializeField] private EquipmentInventorySlot weaponSlot;
    [SerializeField] private EquipmentInventorySlot armorSlot;
    [SerializeField] private EquipmentInventorySlot pantsSlot;
    [SerializeField] private EquipmentInventorySlot glovesSlot;
    [SerializeField] private EquipmentInventorySlot ringSlot;
    [SerializeField] private EquipmentInventorySlot shoesSlot;

    [Header("장비 강화")]
    [Tooltip("탭을 바꿀 때 이전 부위의 '+N%' 강화 결과 표시를 같이 지워주기 위한 참조")]
    [SerializeField] private EquipmentEnhanceButton enhanceButton;

    [Header("인벤토리 정렬")]
    [SerializeField] private TMP_Dropdown sortDropdown;

    private int currentSortOption = 0;

    private EquippedItem selectedEquipment;
    private EquipmentSlot currentSlot;

    public EquipmentSlot CurrentSlot => currentSlot;

    private void OnEnable()
    {
        RefreshEquippedSlots();
        CloseInventory();
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

        // 이전 부위에서 고른 selectedEquipment가 남아있으면, 새로 아무것도 안 고르고
        // 장비 변경을 눌렀을 때 부위가 안 맞는 엉뚱한 장비로 장착 시도하게 되므로 초기화
        selectedEquipment = null;

        // 아이템 순서 정렬엔 항상 기본(0)으로 시작
        currentSortOption = 0;
        if (sortDropdown != null)
        {
            sortDropdown.SetValueWithoutNotify(0);
        }

        statsPanel.SetActive(false);
        inventoryPanel.SetActive(true);
        RefreshInventory(equipmentSlot);
        selectedEquipmentInfo.ShowEquipment(characterSelectController.CurrentCharacter.GetEquipped(equipmentSlot), null);

        if (enhanceButton != null)
        {
            enhanceButton.ClearResult();
        }
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

        // 원본 인벤토리 순서를 유지하기 위해 복사
        items = new List<EquippedItem>(items);

        // 정렬 옵션에 따라 정렬
        if (currentSortOption == 1)
        {
            // 스탯 높은 순
            items.Sort((a, b) => b.TotalRollPercent.CompareTo(a.TotalRollPercent));
        }
        else if (currentSortOption == 2)
        {
            // 스탯 낮은 순
            items.Sort((a, b) => a.TotalRollPercent.CompareTo(b.TotalRollPercent));
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

    public void RefreshCurrentEquipmentDisplay()
    {
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

        CharacterBase currentCharacter = characterSelectController.CurrentCharacter;

        // 다른 캐릭터가 장착 중인 장비인지 먼저 확인
        if (selectedEquipment.EquippedBy != null && selectedEquipment.EquippedBy != currentCharacter)
        {
            alreadyEquippedPanel.SetActive(true);
            return;
        }

        // 장착 가능한 장비라면 기존 확인창 표시
        confirmPanel.SetActive(true);
    }

    // 장비변경 확정창 Yes 버튼 (장비가 변경됌)
    // 장비변경 확정창 Yes 버튼
    public void ConfirmEquipmentChange()
    {
        if (selectedEquipment == null)
            return;

        CharacterBase currentCharacter = characterSelectController.CurrentCharacter;

        // 장착 시도
        bool success = currentCharacter.Equip(currentSlot, selectedEquipment);

        // 다른 캐릭터가 장착 중이거나 장착 조건이 맞지 않으면 중단
        if (!success)
        {
            confirmPanel.SetActive(false);
            return;
        }

        // 장착 성공 후 정보 패널 갱신
        selectedEquipmentInfo.ShowEquipment(selectedEquipment, selectedEquipment
        );

        // 캐릭터 장착 슬롯 갱신
        RefreshEquippedSlots();

        // 현재 인벤토리 슬롯을 다시 생성해서 초록색 테두리 갱신
        RefreshInventory(currentSlot);

        confirmPanel.SetActive(false);
    }

    // 장비변경 확정창 NO 버튼 (그냥 변경 확정창만 닫힘)
    public void CancelEquipmentChange()
    {
        confirmPanel.SetActive(false);
    }

    // 다른 캐릭터가 장착중인 장비에 교체 시도시 뜨는 안내창 닫기 버튼
    public void CloseAlreadyEquippedPanel()
    {
        alreadyEquippedPanel.SetActive(false);
    }

    // 정렬 옵션이 변경됐을 때 호출
    public void OnSortChanged(int option)
    {
        //확인용 로그
        Debug.Log("정렬 옵션 변경: " + option);
        currentSortOption = option;
        // 현재 열려 있는 부위의 인벤토리 다시 생성
        RefreshInventory(currentSlot);
    }
}