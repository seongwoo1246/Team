using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Threading;
public class BootstrapView : MonoBehaviour
{
    [Header("Loading 패널 요소")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TMP_Text progressPercentText;
    [SerializeField] private TMP_Text statusMessageText;

    [Header("팝업 컴포넌트")]
    [SerializeField] private DownloadPopupUI downloadPopupUI;
    [SerializeField] private ErrorPopupUI ErrorPopupUI;

    private void Awake()
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (progressSlider != null) progressSlider.value = 0f;
        if (progressPercentText != null) progressPercentText.text = "Loading...0%";
    }
    public void UpdateState(string message, float progress)
    {
        if (statusMessageText != null) statusMessageText.text = message;
        if (progressSlider != null) progressSlider.value = progress;
        if (progressPercentText != null) progressPercentText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
    }

    public UniTask<bool> ShowDownloadPopupAsync(long totalBytes, CancellationToken ct = default)
    {
        return downloadPopupUI.ShowAsync(totalBytes, ct);
    }

    public UniTask ShowErrorPopupAsync(string message, CancellationToken ct = default)
    {
        return ErrorPopupUI.ShowAndReTryAsync(message, ct);
    }
}
