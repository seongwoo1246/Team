/*
챌린지 스테이지 진입 시, 타이머 밑에 지금 몇 스테이지인지 보여주는 텍스트
ChallengeTimerDisplay랑 표시 규칙(챌린지 모드일 때만 보임) 동일하게 맞춤
*/

using UnityEngine;
using TMPro;

/// <summary>
/// StageManager.ChallengeStarted를 구독해서 현재 챌린지 스테이지 번호를 표시.
/// 챌린지 모드일 때만 보이고 파밍 중엔 숨김
/// </summary>
public sealed class StageNumberDisplay : MonoBehaviour
{
    [Tooltip("스테이지 번호를 표시할 텍스트")]
    [SerializeField] private TextMeshProUGUI stageNumberText;

    // 마지막으로 시작된 챌린지 스테이지 번호
    private int _currentStageNumber = -1;

    // 마지막으로 화면에 그린 스테이지 번호. 안 바뀌면 다시 안 그려서 GC를 피함
    private int _lastDisplayedStageNumber = int.MinValue;

    private bool _wasChallengeMode;

    private void OnEnable()
    {
        if (StageManager.instance != null)
        {
            StageManager.instance.ChallengeStarted += OnChallengeStarted;
        }
    }

    private void OnDisable()
    {
        if (StageManager.instance != null)
        {
            StageManager.instance.ChallengeStarted -= OnChallengeStarted;
        }
    }

    /// <summary>챌린지가 시작(재시작 포함)되면 그 스테이지 번호를 기억해둔다</summary>
    private void OnChallengeStarted(int stageNumber)
    {
        _currentStageNumber = stageNumber;
    }

    private void Update()
    {
        if (stageNumberText == null)
        {
            return;
        }

        bool isChallengeMode = StageManager.instance != null && StageManager.instance.CurrentMode == StageMode.Challenge;

        if (isChallengeMode != _wasChallengeMode)
        {
            _wasChallengeMode = isChallengeMode;
            _lastDisplayedStageNumber = int.MinValue; // 다시 챌린지 들어가면 무조건 새로 그리게 초기화
        }

        // 매 프레임 무조건 실제 모드에 맞춰줌 (조건부로 하면 "우연히 초기값이 같은 경우" 처음에 안 맞춰지는 문제가 있음)
        stageNumberText.gameObject.SetActive(isChallengeMode);

        if (!isChallengeMode || _currentStageNumber == _lastDisplayedStageNumber)
        {
            return;
        }

        _lastDisplayedStageNumber = _currentStageNumber;
        stageNumberText.text = $"{_currentStageNumber} 스테이지";
    }
}
