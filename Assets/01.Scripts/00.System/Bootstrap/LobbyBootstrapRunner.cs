/* 담당자 - 송태훈
로비 씬 진입 시 씬 내 ILoadable 컴포넌트들을 찾아 SceneLoadManager에 자동 등록
ISceneBootstrap을 통해 로비 씬 전환 시 준비 작업을 수행
 */
using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UtilDebug = DebugLogger<LobbyBootstrapRunner>;
public class LobbyBootstrapRunner : MonoBehaviour, ISceneBootstrap
{
    public async UniTask OnSceneReadyAsync()
    {
        GameManager.Instance.ChangeState(GameState.Lobby);
        await UniTask.CompletedTask;
    }

    private async UniTaskVoid Awake()
    {
        var loadables = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<ILoadable>();
        if(SceneLoadManager.Instance != null)
        {
            foreach(var loadable in loadables)
            {
                SceneLoadManager.Instance.RegisterLoadable(loadable);
                UtilDebug.Log($"[Auto-Register] ILoadable 등록 완료: {loadable.GetType().Name} (Order: {loadable.LoadOrder})");
            }
        }
    }

}
