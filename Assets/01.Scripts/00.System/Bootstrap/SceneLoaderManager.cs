using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UtilDebug = DebugLogger<SceneLoaderManager>;
public class SceneLoaderManager : Singleton<SceneLoaderManager>
{
    [Header("로딩 전환 UI(DDOL)")]
    [SerializeField] private CanvasGroup loadingCanvasGroup;
    [SerializeField] private Slider progressSlider;

    private bool isLoading = false;

    protected override void Awake()
    {
        isDDOL = true;
        base.Awake();
        if(loadingCanvasGroup != null )
        {
            loadingCanvasGroup.alpha = 0f;
            loadingCanvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// 로딩 UI로 화면을 덮고, 메모리 정리 및 새 씬 로드, 오브젝트 셋업 콜백 완료 후 화면 열기
    /// </summary>
    public async UniTask LoadSceneFlowAsync(string sceneAddress, Func<UniTask> onSceneReadyAsync = null)
    {
        // 중복 호출 방지
        if(isLoading)
        {
            UtilDebug.LogWarning("이미 씬 전환이 진행 중");
            return;
        }

        isLoading = true;
        var ct = this.GetCancellationTokenOnDestroy();

        try
        {
            if (progressSlider != null)
                progressSlider.value = 0f;

            // 화면 페이드 인
            await FadeLoadingCanvasAsync(1f, 0.3f, ct);

            // 이전 씬 잔여 메모리 해제 및 GC
            AddressableManager.instance.ReleaseAll();
            await Resources.UnloadUnusedAssets().ToUniTask(cancellationToken: ct);
            GC.Collect();

            // Addresables 씬 로드 (게이지 0.0 -> 0.7)
            await UpdateSliderSmoothlyAsync(0.7f, 0.4f, ct);
            await AddressableManager.instance.LoadSceneAsync(sceneAddress);

            // 새 씬 내부 오브젝트 생성 / 풀링/ 셋업 콜백 대기 (0.7 -> 0.9)
            if (onSceneReadyAsync != null)
                await onSceneReadyAsync.Invoke();

            await UpdateSliderSmoothlyAsync(0.9f, 0.2f, ct);

            // 로딩 마무리
            if (progressSlider != null)
                progressSlider.value = 1f;
            await UniTask.Delay(200, cancellationToken: ct);

            // 화면 페이드 아웃
            await FadeLoadingCanvasAsync(0f, 0.3f, ct);
        }
        catch (OperationCanceledException)
        {
            // 작업 취소 처리
        }
        catch (Exception ex)
        {
            UtilDebug.LogError($"씬 전환 중 오류 발생 : {ex.Message}");
            if (loadingCanvasGroup != null)
            {
                loadingCanvasGroup.alpha = 0f;
                loadingCanvasGroup.blocksRaycasts = false;
            }
        }
        finally
        {
            isLoading = false;
        }
    }

    private async UniTask UpdateSliderSmoothlyAsync(float targetValue, float duration, CancellationToken ct)
    {
        if (progressSlider == null) return;


        float startValue = progressSlider.value;
        float elapsed = 0f;

        while(elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            progressSlider.value = Mathf.Lerp(startValue, targetValue, elapsed / duration);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        progressSlider.value = targetValue;
    }

    private async UniTask FadeLoadingCanvasAsync(float targetAlpha, float duration, CancellationToken ct)
    {
        if (loadingCanvasGroup == null) return;

        loadingCanvasGroup.blocksRaycasts = true;
        float startAlpha = loadingCanvasGroup.alpha;
        float time = 0f;

        while(time < duration)
        {
            time += Time.unscaledDeltaTime;
            loadingCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        loadingCanvasGroup.alpha = targetAlpha;
        loadingCanvasGroup.blocksRaycasts = (targetAlpha > 0.9f);
    }
}
