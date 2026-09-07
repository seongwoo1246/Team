/*
MainUI 하단 네비게이션의 Challenge / Lobby 버튼이, 이미 그 모드에 들어가있을 때는
다시 눌러도 의미 없으니(챌린지 재시작/파밍 재시작 낭비) 눌리지 않게 막아주는 스크립트
StageManager에 "파밍 시작" 이벤트가 따로 없어서, ChallengeTimerDisplay/StageNumberDisplay랑
같은 방식으로 Update에서 CurrentMode를 직접 확인함
*/

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StageManager.CurrentMode를 봐서 Challenge/Lobby 버튼의 interactable을 매 프레임 맞춰준다
/// 챌린지 모드 중엔 ChallengeButton을, 파밍 모드 중엔 LobbyButton을 비활성화
/// </summary>
public sealed class BottomNavModeGate : MonoBehaviour
{
    [Tooltip("챌린지 입장 버튼. 이미 챌린지 중이면 비활성화됨")]
    [SerializeField] private Button challengeButton;

    [Tooltip("로비(파밍) 버튼. 이미 파밍 중이면 비활성화됨")]
    [SerializeField] private Button lobbyButton;

    // OnEnable에서 캐싱해두고 그 뒤로는 이 캐시만 씀 (씬 종료 시 .instance 재호출로
    // Singleton<T>가 새 오브젝트를 만들어버리는 문제를 피하기 위함)
    private StageManager _stageManager;

    private void OnEnable()
    {
        _stageManager = StageManager.instance;
    }

    private void Update()
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
