/*
파티 편성 패널(FormationPanel)에 나오는 캐릭터 1명짜리 토글 버튼.
누르면 그 캐릭터를 편성에 넣거나 빼고, 지금 편성 여부에 따라 배경색을 바꿔서 보여준다
*/

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 편성 패널의 캐릭터 1명 토글 버튼. Button의 OnClick에 OnClickToggle()을 연결해서 쓴다
/// </summary>
public sealed class FormationCharacterButton : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("이 버튼이 나타내는 캐릭터")]
    [SerializeField] private CharacterBase character;

    [Tooltip("편성 로직을 처리하는 매니저")]
    [SerializeField] private PartyFormationManager partyFormationManager;

    [Tooltip("편성 여부에 따라 색이 바뀔 배경 이미지 (보통 이 버튼 자신의 Image)")]
    [SerializeField] private Image highlightImage;

    [Header("색상")]
    [Tooltip("편성에 들어가있을 때 배경색")]
    [SerializeField] private Color selectedColor = new Color(0.4f, 0.8f, 1f, 1f);

    [Tooltip("편성에 안 들어가있을 때 배경색")]
    [SerializeField] private Color unselectedColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    /// <summary>버튼 OnClick에 연결. 이 캐릭터를 편성에 넣거나 뺀다</summary>
    public void OnClickToggle()
    {
        if (partyFormationManager == null || character == null)
        {
            return;
        }

        partyFormationManager.ToggleFormation(character);
    }

    /// <summary>지금 편성 여부에 맞춰 배경색을 새로고침한다. FormationPanelController가 호출</summary>
    public void RefreshHighlight()
    {
        if (highlightImage == null || partyFormationManager == null || character == null)
        {
            return;
        }

        highlightImage.color = partyFormationManager.IsInFormation(character) ? selectedColor : unselectedColor;
    }
}
