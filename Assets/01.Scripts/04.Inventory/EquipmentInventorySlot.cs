using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UtilDebug = DebugLogger<EquipmentInventorySlot>;
public class EquipmentInventorySlot : MonoBehaviour
{
    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI enhanceText;
    [SerializeField] private TextMeshProUGUI statText;

    [Header("아이콘")]
    [SerializeField] private Image iconImage;

    [Header("상점 판매 선택 테두리")]
    [SerializeField] private Outline selectedOutline;

    [Header("장착 중 표시 테두리")]
    [SerializeField] private Outline equippedOutline;

    private EquippedItem equippedItem;
    private EquipmentInventoryController controller;
    private Action<EquippedItem> sellClickAction;


    // 일반 장비 인벤토리에서 사용
    public void SetItem(EquippedItem item, EquipmentInventoryController inventoryController)
    {
        equippedItem = item;
        controller = inventoryController;
        sellClickAction = null;

        SetSelected(false);

        if (equippedItem == null || equippedItem.Data == null)
        {
            ClearDisplay();
            return;
        }

        RefreshDisplay();

        Button button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnSlotClicked);
        }
    }


    // 판매 인벤토리에서 사용
    public void SetSellItem(EquippedItem item, Action<EquippedItem> onClick)
    {
        equippedItem = item;
        controller = null;
        sellClickAction = onClick;

        SetSelected(false);

        if (equippedItem == null || equippedItem.Data == null)
        {
            ClearDisplay();
            return;
        }

        RefreshDisplay();

        if (TryGetComponent<Button>(out var button))
        {
            Debug.LogError("버튼 문제");
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnSlotClicked);
        }
    }


    // 캐릭터에게 장착된 장비 표시
    public void SetEquippedItem(EquippedItem item)
    {
        equippedItem = item;
        controller = null;
        sellClickAction = null;

        SetSelected(false);

        if (equippedItem == null || equippedItem.Data == null)
        {
            ClearDisplay();
            return;
        }

        RefreshDisplay();

        if (TryGetComponent<Button>(out var button))
        {
            button.onClick.RemoveAllListeners();
        }
    }


    private void RefreshDisplay()
    {
        if (equippedItem == null || equippedItem.Data == null)
        {
            ClearDisplay();
            return;
        }

        // 김주연 - 등급(하급/중급/상급) 표시 추가
        if (nameText != null)
            nameText.text = "[" + EquipmentGradeHelper.GetDisplayName(equippedItem.Grade) + "] " + equippedItem.Data.NameKr;

        if (enhanceText != null)
            enhanceText.text = $"+{equippedItem.EnhanceLevel}";

        if (statText != null)
            statText.text = $"옵션 +{equippedItem.TotalRollPercent:F1}%";

        // 장비 아이콘 표시
        if (iconImage != null)
        {
            iconImage.sprite = equippedItem.Data.Icon;
            iconImage.enabled = equippedItem.Data.Icon != null;
        }

        // 일반 인벤토리에서만 장착 여부 표시
        if (equippedOutline != null)
        {
            equippedOutline.enabled = controller != null && equippedItem.IsEquipped;
        }
    }


    private void ClearDisplay()
    {
        if (nameText != null)
            nameText.text = string.Empty;

        if (enhanceText != null)
            enhanceText.text = string.Empty;

        if (statText != null)
            statText.text = string.Empty;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (equippedOutline != null)
            equippedOutline.enabled = false;
    }


    // 선택 테두리 표시/숨김
    public void SetSelected(bool selected)
    {
        if (selectedOutline != null)
            selectedOutline.enabled = selected;
    }


    // 일반 인벤토리 슬롯 클릭
    private void OnSlotClicked()
    {
        if (equippedItem == null || controller == null)
            return;

        controller.SelectEquipment(equippedItem);
    }


    // 판매 인벤토리 슬롯 클릭
    private void OnSellSlotClicked()
    {
        if (equippedItem == null)
            return;

        sellClickAction?.Invoke(equippedItem);
    }


    public EquippedItem GetItem()
    {
        return equippedItem;
    }
}