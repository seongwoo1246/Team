// 담당자 - 송태훈
using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UtilDebug = DebugLogger<LobbyBootstrapRunner>;
public class LobbyBootstrapRunner : MonoBehaviour, ISceneBootstrap
{
    public async UniTask OnSceneReadyAsync()
    {
        GameManager.Instance.ChangeState(GameState.Lobby);
        // UI를 배치하는거 나중에 수정하던가?
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
