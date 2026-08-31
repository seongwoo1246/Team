/*
챌린지 남은 시간을 화면에 보여주는 UI. 파밍 중에는 숨겨져 있다가 챌린지에 들어가면 나타남!
*/

using UnityEngine;
using TMPro;

/// <summary>
/// StageManager.ChallengeTimeRemaining을 읽어서 남은 시간을 텍스트로 보여줌
/// 챌린지 모드에서만 보이고 파밍 중엔 숨김
/// </summary>
public sealed class ChallengeTimerDisplay : MonoBehaviour
{
    // "제한 없음" 표시 중임을 나타내는 특수값 (초단위 값과 안 겹치게 음수로)
    private const int UNLIMITED_DISPLAY_MARKER = -2;

    [Tooltip("남은 시간을 표시할 텍스트")]
    [SerializeField] private TextMeshProUGUI timeText;

    // 마지막으로 그린 초 단위 값. 값이 안 바뀌면 텍스트를 다시 안 만들어서 매 프레임 GC를 피함
    private int _lastDisplayedSeconds = int.MinValue;
    private bool _wasChallengeMode;

    private void Update()
    {
        if (timeText == null)
        {
            return;
        }

        bool isChallengeMode = StageManager.instance != null && StageManager.instance.CurrentMode == StageMode.Challenge;

        if (isChallengeMode != _wasChallengeMode)
        {
            timeText.gameObject.SetActive(isChallengeMode);
            _wasChallengeMode = isChallengeMode;
            _lastDisplayedSeconds = int.MinValue; // 모드 전환 직후엔 무조건 다시 그리게 초기화
        }

        if (!isChallengeMode)
        {
            return;
        }

        float remaining = StageManager.instance.ChallengeTimeRemaining;

        if (remaining < 0f)
        {
            if (_lastDisplayedSeconds != UNLIMITED_DISPLAY_MARKER)
            {
                timeText.text = "제한 없음";
                _lastDisplayedSeconds = UNLIMITED_DISPLAY_MARKER;
            }
            return;
        }

        int seconds = Mathf.CeilToInt(remaining);
        if (seconds == _lastDisplayedSeconds)
        {
            return;
        }

        _lastDisplayedSeconds = seconds;
        timeText.text = seconds.ToString() + "초";
    }
}
