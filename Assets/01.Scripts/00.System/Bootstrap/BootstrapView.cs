/* 담담자 - 송태훈
부트스트랩(초기 진입) 단계의 로딩 상태 표시(메시지, 진행도 슬라이더)
다운로드 확인 및 에러 알림 등 초기화 과정에 필요한 팝업 UI 표출 기능을 제공
 */
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
    [SerializeField] private CanvasGroup loadingCanvasGroup;

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

    public void SetLoadingVisible(bool visible)
    {
        if (loadingCanvasGroup == null) return;

        loadingCanvasGroup.alpha = visible ? 1f : 0f;
        loadingCanvasGroup.blocksRaycasts = visible;
        loadingCanvasGroup.interactable = visible;
    }
}
