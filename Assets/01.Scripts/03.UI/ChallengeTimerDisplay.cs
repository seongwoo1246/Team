/*
챌린지 남은 시간을 화면에 보여주는 UI. 파밍 중에는 숨겨져 있다가 챌린지에 들어가면 나타남!
스테이지 클리어 후(선택 화면 대기 중)에는 시간이 의미 없으니 멈춰서 숨긴다.
*/

using UnityEngine;
using TMPro;

/// <summary>
/// StageManager.ChallengeTimeRemaining을 읽어서 남은 시간을 텍스트로 보여줌
/// 챌린지 모드에서만 보이고 파밍 중엔 숨김. 클리어하면 그 순간 멈추고 숨겨짐
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

    // 스테이지를 클리어했거나 실패해서 시간이 멈춰야 하는 상태인지 (선택 화면 대기 중)
    private bool _isAwaitingChoice;

    private void OnEnable()
    {
        if (StageManager.instance != null)
        {
            StageManager.instance.StageCleared += OnStageCleared;
            StageManager.instance.StageFailed += OnStageFailed;
            StageManager.instance.ChallengeStarted += OnChallengeStarted;
        }
    }

    private void OnDisable()
    {
        if (StageManager.instance != null)
        {
            StageManager.instance.StageCleared -= OnStageCleared;
            StageManager.instance.StageFailed -= OnStageFailed;
            StageManager.instance.ChallengeStarted -= OnChallengeStarted;
        }
    }

    /// <summary>클리어된 순간 시간 표시를 멈추고 숨긴다</summary>
    private void OnStageCleared(int stageNumber)
    {
        _isAwaitingChoice = true;
    }

    /// <summary>실패한 순간에도 클리어와 동일하게 시간 표시를 멈추고 숨긴다</summary>
    private void OnStageFailed(int stageNumber)
    {
        _isAwaitingChoice = true;
    }

    /// <summary>새 챌린지가 시작되면 정지 상태를 풀어 다시 정상적으로 카운트하게 한다</summary>
    private void OnChallengeStarted(int stageNumber)
    {
        _isAwaitingChoice = false;
    }

    private void Update()
    {
        if (timeText == null)
        {
            return;
        }

        bool isChallengeMode = StageManager.instance != null && StageManager.instance.CurrentMode == StageMode.Challenge;

        if (isChallengeMode != _wasChallengeMode)
        {
            _wasChallengeMode = isChallengeMode;
            _lastDisplayedSeconds = int.MinValue; // 모드 전환 직후엔 무조건 다시 그리게 초기화

            if (!isChallengeMode)
            {
                _isAwaitingChoice = false; // 파밍으로 나가면 다음 챌린지를 위해 정지 상태도 초기화
            }
        }

        // 클리어/실패한 뒤(선택 화면 대기 중)에는 시간이 의미 없으니 그냥 숨겨둔다
        if (!isChallengeMode || _isAwaitingChoice)
        {
            timeText.gameObject.SetActive(false);
            return;
        }

        timeText.gameObject.SetActive(true);

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
