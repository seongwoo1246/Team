using UnityEngine;
using TMPro;

public sealed class EquipmentEnhanceButton : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("지금 어느 캐릭터/부위를 보고 있는지 알기 위한 참조")]
    [SerializeField] private CharacterSelectController characterSelectController;

    [Tooltip("지금 열려있는 부위(currentSlot)를 알려줄 인벤토리 컨트롤러")]
    [SerializeField] private EquipmentInventoryController inventoryController;

    [Header("확인창")]
    [Tooltip("EquipmentConfirmPanel과 똑같이 생긴 '강화하시겠습니까?' 확인 패널")]
    [SerializeField] private GameObject confirmPanel;

    [Header("강화 결과 표시")]
    [Tooltip("강화 성공 시 '+N.N%' 를 보여줄 텍스트 (노란색 등으로 눈에 띄게 색 설정은 인스펙터에서)")]
    [SerializeField] private TextMeshProUGUI enhanceResultText;

    private void Awake()
    {
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }

        if (enhanceResultText != null)
        {
            enhanceResultText.text = string.Empty;
        }
    }

    public void OnClickEnforce()
    {
        if (characterSelectController == null || inventoryController == null)
        {
            return;
        }

        CharacterBase character = characterSelectController.CurrentCharacter;
        if (character == null || character.GetEquipped(inventoryController.CurrentSlot) == null)
        {
            return;
        }

        if (confirmPanel != null)
        {
            confirmPanel.SetActive(true);
        }
    }

    public void OnConfirmYes()
    {
        if (characterSelectController != null && inventoryController != null)
        {
            CharacterBase character = characterSelectController.CurrentCharacter;
            EquippedItem item = character != null ? character.GetEquipped(inventoryController.CurrentSlot) : null;

            float beforePercent = item != null ? item.TotalRollPercent : 0f;
            bool success = character != null && character.TryEnhanceEquipped(inventoryController.CurrentSlot);
            float afterPercent = item != null ? item.TotalRollPercent : 0f;

            if (enhanceResultText != null)
            {
                enhanceResultText.text = success ? $"+{(afterPercent - beforePercent):F1}%" : string.Empty;
            }

            if (character != null)
            {
                inventoryController.RefreshCurrentEquipmentDisplay();
            }
        }

        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }
    }

    public void OnConfirmNo()
    {
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }
    }

    public void ClearResult()
    {
        if (enhanceResultText != null)
        {
            enhanceResultText.text = string.Empty;
        }
    }
}
