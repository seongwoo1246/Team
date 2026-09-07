/*
화면 상단 왼쪽에 플레이어 레벨을 보여주는 텍스트
*/

using UnityEngine;
using TMPro;

/// <summary>
/// PlayerLevelSystem.Level을 읽어서 "Lv.N" 형태로 보여줌. 레벨이 안 바뀌면 다시 안 그려서 GC를 피함
/// </summary>
public sealed class PlayerLevelDisplay : MonoBehaviour
{
    [Tooltip("레벨을 표시할 텍스트")]
    [SerializeField] private TextMeshProUGUI levelText;

    // 마지막으로 그린 레벨. 안 바뀌면 다시 안 그림
    private int _lastDisplayedLevel = int.MinValue;

    private void Update()
    {
        if (levelText == null || PlayerLevelSystem.instance == null)
        {
            return;
        }

        int level = PlayerLevelSystem.instance.Level;
        if (level == _lastDisplayedLevel)
        {
            return;
        }

        _lastDisplayedLevel = level;
        levelText.text = $"Lv.{level}";
    }
}
