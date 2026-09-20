// 작성자: 김주연
/*
MainUI 하단 네비게이션의 Challenge / Lobby 버튼이, 이미 그 모드에 들어가있을 때는
다시 눌러도 의미 없으니(챌린지 재시작/파밍 재시작 낭비) 눌리지 않게 막아주는 스크립트

StageManager.ModeChanged를 구독해서 모드가 바뀔 때만 두 버튼의 interactable을 맞춘다
(예전엔 파밍 시작 이벤트가 없어서 매 프레임 Update에서 CurrentMode를 폴링했었음)
*/

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UtilDebug = DebugLogger<BottomNavModeGate>;

/// <summary>
/// StageManager.CurrentMode를 봐서 Challenge/Lobby 버튼의 interactable을 맞춰준다
/// 챌린지 모드 중엔 ChallengeButton을, 파밍 모드 중엔 LobbyButton을 비활성화
/// </summary>
public sealed class BottomNavModeGate : MonoBehaviour, ILoadable
{
    [Tooltip("챌린지 입장 버튼. 이미 챌린지 중이면 비활성화됨")]
    [SerializeField] private Button challengeButton;

    [Tooltip("로비(파밍) 버튼. 이미 파밍 중이면 비활성화됨")]
    [SerializeField] private Button lobbyButton;

    private StageManager _stageManager;

    public int LoadOrder => 40;

    private void Awake()
    {
        SceneLoadManager.Instance.RegisterLoadable(this);
    }

    private void OnEnable()
    {
        TryBindStageManager();
        ApplyCurrentMode();
    }

    private void OnDisable()
    {
        UnbindStageManager();
    }

    public UniTask OnSceneLoadCreate(SceneId scene) => UniTask.CompletedTask;

    public void Init(SceneId scene)
    {
        if (scene != SceneId.LobbySceneTest) return;
        try { TryBindStageManager(); ApplyCurrentMode(); }
        catch (System.Exception ex) { UtilDebug.LogError($"초기화 중 예외 발생: {ex.Message}"); }
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
            _stageManager.ModeChanged -= OnModeChanged;
            _stageManager.ModeChanged += OnModeChanged;
        }
    }

    private void UnbindStageManager()
    {
        if (_stageManager != null)
        {
            _stageManager.ModeChanged -= OnModeChanged;
        }
    }

    private void OnModeChanged(StageMode mode)
    {
        ApplyCurrentMode();
    }

    private void ApplyCurrentMode()
    {
        if (_stageManager == null)
        {
            return;
        }

        bool isChallengeMode = _stageManager.CurrentMode == StageMode.Challenge;

        if (challengeButton != null)
        {
            challengeButton.interactable = !isChallengeMode;
        }

        if (lobbyButton != null)
        {
            lobbyButton.interactable = isChallengeMode;
        }
    }
}
