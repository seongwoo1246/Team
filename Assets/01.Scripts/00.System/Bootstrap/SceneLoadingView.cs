/* 담당자 - 송태훈
 */ 

using Cysharp.Threading.Tasks;
using UnityEngine;

public class SceneLoadingView : MonoBehaviour
{
    [SerializeField] private CanvasGroup loadingCanvasGroup;
    [SerializeField] private UnityEngine.UI.Slider progressSlider;

    private void Awake()
    {
        ForceHide();
    }
    public void ResetProgress()
    {
        if (progressSlider != null) progressSlider.value = 0f;
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
        if (progressSlider == null) return;

        float startValue = progressSlider.value;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            progressSlider.value = Mathf.Lerp(startValue, targetValue, elapsed / duration);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        progressSlider.value = targetValue;
    }

    public void ForceHide()
    {
        if(loadingCanvasGroup != null)
        {
            loadingCanvasGroup.alpha = 0f;
            loadingCanvasGroup.blocksRaycasts = false;
        }
        ResetProgress();
    }
}
