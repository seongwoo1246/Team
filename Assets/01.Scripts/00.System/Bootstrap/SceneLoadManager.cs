/* 담담자 - 송태훈

 */

using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
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
        ServiceLocator.Register<SceneLoadManager>(this);
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

    #endregion
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

        var addressableMgr = ServiceLocator.Get<AddressableManager>();
        if (addressableMgr == null) return;

        isLoading = true;
        var ct = this.GetCancellationTokenOnDestroy();

        try
        {
            // 1. 화면 암전 페이드 인
            if (loadingView != null)
            {
                loadingView.ResetProgress();
                await loadingView.FadeAsync(1f, 0.25f, ct);
            }

            // 2. 파괴된 객체 정리 및 이전 씬 정리 ( OrderBy 역순 )
            NotifySceneDestroyToLoadables(CurrentScene);

            // 3. 씬 종속 서비스 및 에셋 메모리 정리
            ServiceLocator.ClearSceneLocalServices();
            addressableMgr.ReleaseAll();
            await Resources.UnloadUnusedAssets().ToUniTask(cancellationToken: ct);
            GC.Collect();

            await loadingView.UpdateSliderSmoothAsync(0.3f, 0.2f, ct);

            // 4. Addressables를 통한 새 씬 비동기 로드
            await AddressableManager.instance.LoadSceneAsync(nextScene.ToString());
            await loadingView.UpdateSliderSmoothAsync(0.5f, 0.2f, ct);

            // 5. 새 씬 매니저 순차 초기화 ( 오름차순 )
            var sortedLoadables = _loadables.OrderBy(x => x.LoadOrder).ToList();

            // 비동기 에셋 로드 및 생성
            for (int i = 0; i < sortedLoadables.Count; i++)
            {
                await sortedLoadables[i].OnSceneLoadCreate(nextScene);
                float stepProgress = 0.5f + (0.35f * ((float)(i + 1) / sortedLoadables.Count));
                await loadingView.UpdateSliderSmoothAsync(stepProgress, 0.05f, ct);
            }

            foreach (var loadable in sortedLoadables)
            {
                loadable.Init(nextScene);
            }

            _currentScene = nextScene;
            await loadingView.UpdateSliderSmoothAsync(1.0f, 0.15f, ct);
            await UniTask.Delay(100, cancellationToken: ct);
            await loadingView.FadeAsync(0f, 0.25f, ct);
        }
        catch (OperationCanceledException)
        {
            // 작업 취소 처리
        }
        catch (Exception ex)
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
}
