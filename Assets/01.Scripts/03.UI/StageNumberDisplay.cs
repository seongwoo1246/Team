// 작성자: 김주연
/*
챌린지 스테이지 진입 시, 타이머 밑에 지금 몇 스테이지인지 보여주는 텍스트
ChallengeTimerDisplay랑 표시 규칙(챌린지 모드일 때만 보임) 동일하게 맞춤
*/

using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;

/// <summary>
/// StageManager.ChallengeStarted를 구독해서 현재 챌린지 스테이지 번호를 표시.
/// 챌린지 모드일 때만 보이고 파밍 중엔 숨김
/// </summary>
public sealed class StageNumberDisplay : MonoBehaviour, ILoadable
{
    [Tooltip("스테이지 번호를 표시할 텍스트")]
    [SerializeField] private TextMeshProUGUI stageNumberText;

    // 마지막으로 시작된 챌린지 스테이지 번호
    private int _currentStageNumber = -1;

    // 마지막으로 화면에 그린 스테이지 번호. 안 바뀌면 다시 안 그려서 GC를 피함
    private int _lastDisplayedStageNumber = int.MinValue;

    private StageManager _stageManager;

    public int LoadOrder => 40;

    private void Awake()
    {
        SceneLoadManager.Instance.RegisterLoadable(this);
    }

    private void OnEnable()
    {
        TryBindStageManager();
        ApplyCurrentState();
    }

    private void OnDisable()
    {
        UnbindStageManager();
    }

    public UniTask OnSceneLoadCreate(SceneId scene) => UniTask.CompletedTask;

    public void Init(SceneId scene)
    {
        if (scene != SceneId.LobbySceneTest) return;
        try { TryBindStageManager(); ApplyCurrentState(); }
        catch (System.Exception ex) { DebugLogger<StageNumberDisplay>.LogError($"초기화 중 예외 발생: {ex.Message}"); }
    }

    public void OnSceneDestory(SceneId scene)
    {
        UnbindStageManager();
    }

    private void TryBindStageManager()
    {
        if (ServiceLocator.TryGet<StageManager>(out StageManager stageMng))
        {
            _stageManager = stageMng;
            _stageManager.ChallengeStarted -= OnChallengeStarted;
            _stageManager.ChallengeStarted += OnChallengeStarted;
            _stageManager.ModeChanged -= OnModeChanged;
            _stageManager.ModeChanged += OnModeChanged;
        }
    }

    private void UnbindStageManager()
    {
        if (_stageManager != null)
        {
            _stageManager.ChallengeStarted -= OnChallengeStarted;
            _stageManager.ModeChanged -= OnModeChanged;
        }
    }

    /// <summary>챌린지가 시작(재시작 포함)되면 그 스테이지 번호를 기억하고 텍스트를 갱신한다</summary>
    private void OnChallengeStarted(int stageNumber)
    {
        _currentStageNumber = stageNumber;
        ApplyCurrentState();
    }

    /// <summary>모드가 바뀌면(파밍 ↔ 챌린지) 보임/숨김을 다시 맞춘다</summary>
    /// <param name="mode">바뀐 후의 모드 (여기선 안 씀 - CurrentMode로 직접 확인)</param>
    private void OnModeChanged(StageMode mode)
    {
        ApplyCurrentState();
    }

    /// <summary>
    /// 현재 모드가 챌린지면 텍스트를 켜고 스테이지 번호를 그린다. 파밍이면 숨긴다
    /// </summary>
    private void ApplyCurrentState()
    {
        if (stageNumberText == null || _stageManager == null)
        {
            return;
        }

        bool isChallengeMode = _stageManager.CurrentMode == StageMode.Challenge;
        stageNumberText.gameObject.SetActive(isChallengeMode);

        if (!isChallengeMode || _currentStageNumber == _lastDisplayedStageNumber)
        {
            return;
        }

        _lastDisplayedStageNumber = _currentStageNumber;
        stageNumberText.text = $"{_currentStageNumber} 스테이지";
    }
}
