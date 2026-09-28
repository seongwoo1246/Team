/* 담당자 - 송태훈
씬 전환 전/후의 로딩 화면 페이드 인/아웃 연출 처리
 */
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SceneLoadingView : MonoBehaviour
{
    [SerializeField] private CanvasGroup loadingCanvasGroup;
    [SerializeField] private UnityEngine.UI.Slider progressSlider;
    [SerializeField] private TMPro.TextMeshProUGUI progressText;

    private void Awake()
    {
        ForceHide();
    }
    public void ResetProgress()
    {
        if (progressSlider != null) progressSlider.value = 0f;
        UpdateProgressText(0f);
    }
    private void UpdateProgressText(float value)
    {
        if (progressText != null)
        {
            progressText.text = $"Loading... {(int)(value * 100f)}%";
        }
    }


    public async UniTask FadeAsync(float targetAlpha, float duration, System.Threading.CancellationToken ct)
    {
        if (loadingCanvasGroup == null) return;

        loadingCanvasGroup.blocksRaycasts = true;
        float startAlpha = loadingCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            loadingCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        loadingCanvasGroup.alpha = targetAlpha;
        loadingCanvasGroup.blocksRaycasts = (targetAlpha > 0.9f);
    }

    public async UniTask UpdateSliderSmoothAsync(float targetValue, float duration, System.Threading.CancellationToken ct)
    {
        if (this == null || progressSlider == null || ct.IsCancellationRequested) return;

        try
        {
            float startValue = progressSlider.value;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                // 오브젝트가 도중에 파괴되었거나 토큰 취소 시 안전 탈출
                if (this == null || progressSlider == null || ct.IsCancellationRequested) return;

                elapsed += Time.unscaledDeltaTime;
                float current = Mathf.Lerp(startValue, targetValue, elapsed / duration);
                progressSlider.value = current;
                UpdateProgressText(current);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            if (this != null && progressSlider != null)
            {
                progressSlider.value = targetValue;
                UpdateProgressText(targetValue);
            }
        }
        catch (System.OperationCanceledException)
        {
            // 씬 전환/오브젝트 파괴로 인한 작업 취소 시 정상 종료 처리
        }
        catch (System.Exception ex) when (ex.Message.Contains("DestroyCancellation"))
        {
            // UniTask 내부 토큰 파괴 예외 방어
        }
    }

    public void ForceHide()
    {
        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.alpha = 0f;
            loadingCanvasGroup.blocksRaycasts = false;
        }
        ResetProgress();
    }
}
