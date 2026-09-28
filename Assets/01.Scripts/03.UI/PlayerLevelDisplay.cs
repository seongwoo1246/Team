// 작성자: 김주연
/*
화면 상단 왼쪽에 플레이어 레벨 + 경험치 바를 보여주는 텍스트
*/

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PlayerLevelSystem.Level을 읽어서 "Lv.N" 형태로 보여주고, 경험치 진행률을 초록색 바 +
/// "현재/다음레벨필요" 텍스트로 같이 보여줌. 값이 안 바뀌면 다시 안 그려서 GC를 피함
/// </summary>
public sealed class PlayerLevelDisplay : MonoBehaviour
{
    [Tooltip("레벨을 표시할 텍스트")]
    [SerializeField] private TextMeshProUGUI levelText;

    [Tooltip("경험치 바에서 초록색으로 채워지는 Image (Image Type = Filled, Horizontal)")]
    [SerializeField] private Image expFillImage;

    [Tooltip("현재경험치/다음레벨필요경험치를 보여줄 텍스트")]
    [SerializeField] private TextMeshProUGUI expText;

    // 마지막으로 그린 레벨. 안 바뀌면 다시 안 그림
    private int _lastDisplayedLevel = int.MinValue;

    // 마지막으로 그린 경험치(정수로 반올림해서 비교). 안 바뀌면 다시 안 그림
    private int _lastDisplayedExp = int.MinValue;

    private void Update()
    {
        if (PlayerLevelSystem.Instance == null)
        {
            return;
        }

        int level = PlayerLevelSystem.Instance.Level;
        if (levelText != null && level != _lastDisplayedLevel)
        {
            _lastDisplayedLevel = level;
            levelText.text = $"Lv.{level}";
        }

        int currentExp = Mathf.FloorToInt(PlayerLevelSystem.Instance.CurrentExp);
        if (currentExp == _lastDisplayedExp)
        {
            return;
        }

        _lastDisplayedExp = currentExp;
        int requiredExp = Mathf.FloorToInt(PlayerLevelSystem.Instance.ExpRequiredForNextLevel);

        if (expFillImage != null)
        {
            expFillImage.fillAmount = PlayerLevelSystem.Instance.ExpProgressRatio;
        }

        if (expText != null)
        {
            expText.text = $"{currentExp}/{requiredExp}";
        }
    }
}
