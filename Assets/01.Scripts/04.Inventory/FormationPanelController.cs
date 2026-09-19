// 작성자: 김주연
/*
파티 편성 패널을 여닫고, 정확히 3명이 편성됐을 때만 완료 버튼을 누를 수 있게 관리한다
캐릭터 화면의 Slot_Skills를 누르면 이 패널이 열리고, 5명 중 원하는 캐릭터를 눌러서 넣었다 뺐다 하다가
정확히 3명이 되면 완료 버튼이 활성화되어 누를 수 있음 (그 전까진 못 누름)
*/

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// EquipmentInventoryController와 같은 자리(CharacterPanel)에 붙여서, CharacterPanel이 다시 켜질 때마다
/// OnEnable로 편성 패널을 닫힌 상태로 리셋한다
/// </summary>
public sealed class FormationPanelController : MonoBehaviour
{
    // 파티 편성 최대 인원 (PartyFormationManager와 동일한 값)
    private const int REQUIRED_COUNT = 3;

    #region 김주연 - ServiceLocator로 매니저 연결
    // ServiceLocator로 조회
    //이 패널은 CharacterPanel 열릴 때마다 OnEnable이 반복 실행
    // 캐싱 안하고 OnEnable마다 다시 조회
    private PartyFormationManager partyFormationManager;
    #endregion

    [Header("연결")]
    [Tooltip("Slot_Skills를 누르면 열리는 편성 패널")]
    [SerializeField] private GameObject formationPanel;

    [Tooltip("편성 패널 안의 캐릭터 토글 버튼 5개")]
    [SerializeField] private FormationCharacterButton[] characterButtons;

    [Tooltip("정확히 3명 편성됐을 때만 눌리는 완료 버튼")]
    [SerializeField] private Button doneButton;

    [Tooltip("지금 몇 명 편성됐는지 보여주는 텍스트 (예: 파티 편성 (2/3))")]
    [SerializeField] private TextMeshProUGUI countText;

    private void OnEnable()
    {
        if (formationPanel != null)
        {
            formationPanel.SetActive(false);
        }

        if (!ServiceLocator.TryGet<PartyFormationManager>(out partyFormationManager))
        {
            DebugLogger<FormationPanelController>.LogError("PartyFormationManager를 ServiceLocator에서 찾을 수 없습니다.");
            return;
        }

        partyFormationManager.FormationChanged += OnFormationChanged;
    }

    private void OnDisable()
    {
        if (partyFormationManager != null)
        {
            partyFormationManager.FormationChanged -= OnFormationChanged;
        }
    }

    /// <summary>편성이 바뀔 때마다(캐릭터 버튼 클릭 등) 패널 표시를 새로고침</summary>
    /// <param name="formation">새 편성 상태 (여기선 값 자체는 안 쓰고 새로고침 트리거로만 사용)</param>
    private void OnFormationChanged(CharacterBase[] formation)
    {
        RefreshAll();
    }

    /// <summary>Slot_Skills 버튼 OnClick에 연결. 편성 패널을 연다</summary>
    public void OnClickOpen()
    {
        if (formationPanel != null)
        {
            formationPanel.SetActive(true);
        }

        RefreshAll();
    }

    /// <summary>완료 버튼 OnClick에 연결. 정확히 3명일 때만 호출되도록 완료 버튼 자체가 그때만 활성화됨</summary>
    public void OnClickDone()
    {
        if (formationPanel != null)
        {
            formationPanel.SetActive(false);
        }
    }

    /// <summary>캐릭터 버튼 5개의 색상과 완료 버튼 활성화 여부, 인원수 텍스트를 전부 새로고침</summary>
    private void RefreshAll()
    {
        if (characterButtons != null)
        {
            for (int i = 0; i < characterButtons.Length; i++)
            {
                if (characterButtons[i] != null)
                {
                    characterButtons[i].RefreshHighlight();
                }
            }
        }

        int count = CountFormation();

        if (doneButton != null)
        {
            doneButton.interactable = count == REQUIRED_COUNT;
        }

        if (countText != null)
        {
            countText.text = "파티 편성 (" + count + "/" + REQUIRED_COUNT + ")";
        }
    }

    /// <summary>지금 편성된 인원 수를 센다 (빈 슬롯은 null이라 제외)</summary>
    private int CountFormation()
    {
        if (partyFormationManager == null)
        {
            return 0;
        }

        CharacterBase[] formation = partyFormationManager.GetFormation();
        int count = 0;
        for (int i = 0; i < formation.Length; i++)
        {
            if (formation[i] != null)
            {
                count++;
            }
        }

        return count;
    }
}
