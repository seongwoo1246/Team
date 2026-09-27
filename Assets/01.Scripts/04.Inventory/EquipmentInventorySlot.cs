/*
 담당자 - 홍준호
 아이템 슬롯 인벤토리, 상점창 표기용 스크립트
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentInventorySlot : MonoBehaviour
{
    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI enhanceText;
    [SerializeField] private TextMeshProUGUI statText;

    [Header("아이콘")]
    [SerializeField] private Image iconImage;

    [Header("프레임")]
    [SerializeField] private Image frameImage;

    // 아이템 기본 프레임
    [SerializeField] private Sprite normalFrame;

    // 장비 장착시 녹색 프레임
    [SerializeField] private Sprite equippedFrame;

    // 판매창 선택시 적색 프레임
    [SerializeField] private Sprite selectedFrame;

    private EquippedItem equippedItem;
    private EquipmentInventoryController controller;
    private Action<EquippedItem> sellClickAction;

    private bool isSelected;
    private bool isEquippedDisplay;


    // 일반 장비 인벤토리에서 사용
    public void SetItem(EquippedItem item, EquipmentInventoryController inventoryController)
    {
        equippedItem = item;
        controller = inventoryController;
        sellClickAction = null;
        isEquippedDisplay = false;

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
        isEquippedDisplay = false;

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
            button.onClick.AddListener(OnSellSlotClicked);
        }
    }


    // 캐릭터에게 장착된 장비 표시
    public void SetEquippedItem(EquippedItem item)
    {
        equippedItem = item;
        controller = null;
        sellClickAction = null;
        isEquippedDisplay = true;

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

        if (nameText != null)
        {
            nameText.text = "[" + EquipmentGradeHelper.GetDisplayName(equippedItem.Grade) + "] " + equippedItem.Data.NameKr;
        }

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

        // 프레임 상태 갱신
        RefreshFrame();
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

        if (frameImage != null)
            frameImage.sprite = normalFrame;
    }


    // 프레임 상태 갱신
    private void RefreshFrame()
    {
        if (frameImage == null)
            return;

        // 판매창에서 선택된 아이템 → 빨간색
        if (isSelected)
        {
            frameImage.sprite = selectedFrame;
            return;
        }

        // 장착 중인 아이템 → 초록색
        if (isEquippedDisplay ||
            (controller != null && equippedItem != null && equippedItem.IsEquipped))
        {
            frameImage.sprite = equippedFrame;
            return;
        }

        // 기본 상태 → 회색
        frameImage.sprite = normalFrame;
    }


    // 선택 프레임 표시/숨김
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        RefreshFrame();
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

    // 가챠 결과창
    public void SetResultItem(EquippedItem item)
    {
        equippedItem = item;
        controller = null;
        sellClickAction = null;

        isSelected = false;
        isEquippedDisplay = false;

        if (equippedItem == null || equippedItem.Data == null)
        {
            ClearDisplay();
            return;
        }

        RefreshDisplay();

        // 결과창에서는 클릭 기능 없음
        if (TryGetComponent<Button>(out var button))
        {
            button.onClick.RemoveAllListeners();
        }
    }
}