// 담당자 - 송태훈
using Cysharp.Threading.Tasks;
using UnityEngine;
using UtilDebug = DebugLogger<LobbyBootstrapRunner>;
public class LobbyBootstrapRunner : MonoBehaviour, ISceneBootstrap
{
    [SerializeField] private PartyFormationManager partyFormationManager;
    public async UniTask OnSceneReadyAsync()
    {
        UtilDebug.Log("[LobbyBootstrapRunner] 로비 씬 내부 컴포넌트 세팅 시작");
        // 1. 각 필요한 시스템 서비스 등록 
        SceneLoadManager.Instance.RegisterLoadable(partyFormationManager);

        // 2. UI 및 Lobby 씬 오브젝트 부착

        await UniTask.Yield();
    }
}
