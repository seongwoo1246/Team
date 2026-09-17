/* 담담자 - 송태훈

 */
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UtilDebug = DebugLogger<SceneLoadManager>;
public class SceneLoadManager : Singleton<SceneLoadManager>
{
    [Header("로딩 전환 UI(DDOL)")]
    [SerializeField] private SceneLoadingView loadingView;

    private readonly List<ILoadable> _loadables = new();
    private SceneId _currentScene = SceneId.None;
    private bool isLoading = false;
    public SceneId CurrentScene => _currentScene;

    protected override void Awake()
    {
        isDDOL = true;
        base.Awake();
    }

    public void RegisterLoadable(ILoadable loadable)
    {
        if (_loadables == null) return;
        if (!_loadables.Contains(loadable))
            _loadables.Add(loadable);
    }
    public void UnregisterLoadable(ILoadable loadable)
    {
        if (_loadables == null) return;
        _loadables.Remove(loadable);
    }

    #region 초기화 파이프라인
    /// <summary>
    /// 씬 전환 없이 현재 등록된 매니저들을 일괄 초기화
    /// </summary>
    /// <param name="currentScene"></param>
    /// <returns></returns>
    public async UniTask InitailizeLoadableAsync(SceneId currentScene)
    {
        _currentScene = currentScene;
        CleanupDeadLoadables();

        var sortedLoadables = _loadables.OrderBy(x => x.LoadOrder).ToList();

        for (int i = 0; i < sortedLoadables.Count; i++)
        {
            await sortedLoadables[i].OnSceneLoadCreate(currentScene);
        }

        foreach (var loadable in sortedLoadables)
        {
            loadable.Init(currentScene);
        }
    }
    private void CleanupDeadLoadables()
    {
        _loadables.RemoveAll(x => x is UnityEngine.Object unityObj && unityObj == null);
    }
    #endregion

    #region 씬 전환 메인 파이프라인
    /// <summary>
    /// 새 씬 로드 및 라이프 사이클 실행
    /// </summary>
    public async UniTask LoadSceneFlowAsync(SceneId nextScene)
    {
        // 중복 호출 방지
        if (isLoading)
        {
            UtilDebug.LogWarning("이미 씬 전환이 진행 중");
            return;
        }
        isLoading = true;


        try
        {
            // 1. 화면 암전 페이드 인
            if (loadingView != null)
            {
                loadingView.ResetProgress();
                await loadingView.FadeAsync(1f, 0.25f, this.destroyCancellationToken);
            }

            // 2. 파괴된 객체 정리 및 이전 씬 정리 ( OrderBy 역순 )
            NotifySceneDestroyToLoadables(CurrentScene);

            // 3. 씬 종속 서비스 및 에셋 메모리 정리
            await MemoryCleanupAsync();

            if (loadingView != null)
                await loadingView.UpdateSliderSmoothAsync(0.3f, 0.2f, this.destroyCancellationToken);

            // 4. Addressables를 통한 새 씬 비동기 로드
            await AddressableManager.Instance.LoadSceneAsync(nextScene.ToString());
            if (loadingView != null)
                await loadingView.UpdateSliderSmoothAsync(0.5f, 0.2f, this.destroyCancellationToken);

            // 5. 새 씬 매니저 순차 초기화 ( 오름차순 )
            await LTSManagerInitAsync(nextScene);

            // 6. 씬 내부 BootstrapRunner 실행 및 셋업 완료 대기
            await LTSBootstrapRunnerAsync(nextScene);


            _currentScene = nextScene;
            if (loadingView != null)
            {
                await loadingView.UpdateSliderSmoothAsync(1.0f, 0.15f, this.destroyCancellationToken);
                await UniTask.Delay(100, cancellationToken: this.destroyCancellationToken);
                await loadingView.FadeAsync(0f, 0.25f, this.destroyCancellationToken);
            }
            UtilDebug.LogError("씬 전환 완료");
        }
        catch (System.OperationCanceledException)
        {
            // 작업 취소 처리
        }
        catch (System.Exception ex)
        {
            UtilDebug.LogError($"씬 전환 중 오류 발생 : {ex.Message}");
            loadingView.ForceHide();
        }
        finally
        {
            isLoading = false;
        }
    }
    private void NotifySceneDestroyToLoadables(SceneId sceneId)
    {
        CleanupDeadLoadables();

        for (int i = _loadables.Count - 1; i >= 0; i--)
        {
            _loadables[i].OnSceneDestory(_currentScene);
        }
    }
    #endregion

    /// <summary>
    /// 씬 종속 서비스 및 에셋 메모리 정리 비동기 메서드
    /// </summary>
    private async UniTask MemoryCleanupAsync()
    {
        ServiceLocator.ClearSceneLocalServices();
        AddressableManager.Instance.ReleaseAll();
        await Resources.UnloadUnusedAssets().ToUniTask(cancellationToken: this.destroyCancellationToken);
        System.GC.Collect();
    }

    /// <summary>
    /// 새 씬 매니저 순차 초기화 비동기 메서드 ( 오름차순 )
    /// </summary>
    private async UniTask LTSManagerInitAsync(SceneId scene)
    {
        var sortedLoadables = _loadables.OrderBy(x => x.LoadOrder).ToList();

        // 비동기 에셋 로드 및 생성
        for (int i = 0; i < sortedLoadables.Count; i++)
        {
            await sortedLoadables[i].OnSceneLoadCreate(scene);
            if (loadingView != null)
            {
                float stepProgress = 0.5f + (0.35f * ((float)(i + 1) / sortedLoadables.Count));
                await loadingView.UpdateSliderSmoothAsync(stepProgress, 0.05f, this.destroyCancellationToken);
            }
        }

        foreach (var loadable in sortedLoadables)
        {
            loadable.Init(scene);
        }
    }

    /// <summary>
    /// // 6. 씬 내부 BootstrapRunner 실행 및 셋업 완료 대기 비동기 메서드
    /// </summary>
    private async UniTask LTSBootstrapRunnerAsync(SceneId scene)
    {
        var bootstrapRunner = UnityEngine.Object.FindAnyObjectByType<MonoBehaviour>() as ISceneBootstrap;
        if (bootstrapRunner == null)
        {
            var allRunners = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var mono in allRunners)
            {
                if (mono is ISceneBootstrap target)
                {
                    bootstrapRunner = target;
                    break;
                }
            }
        }

        if (bootstrapRunner != null)
        {
            UtilDebug.Log($"[{scene}] ISceneBootstrap 감지 - 씬 셋업 대기 시작");
            await bootstrapRunner.OnSceneReadyAsync();
            UtilDebug.Log($"[{scene}] ISceneBootstrap 씬 셋업 완료");
        }
    }
}
