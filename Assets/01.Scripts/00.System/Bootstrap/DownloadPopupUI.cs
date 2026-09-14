/*
 담담자 - 송태훈
 
 */
using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DownloadPopupUI : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private TMP_Text downloadSizeText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    /// <summary>
    /// 다운로드 용량을 표기하고 다운(true) 또는 X(false) 입력을 비동기로 반환
    /// </summary>
    public async UniTask<bool> ShowAsync(long totalBytes, CancellationToken ct = default)
    {
        downloadSizeText.text = $"{totalBytes / (1024f * 1024f):F1} MB";
        gameObject.SetActive(true);

        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        try
        {
            var confirmTask = confirmButton.OnClickAsync(ct);
            var cancelTask = cancelButton.OnClickAsync(ct);

            int result = await UniTask.WhenAny(confirmTask, cancelTask);

            return result == 0; // confirm이면 true, cancel이면 false
        }
        finally
        {
            linkedCts.Cancel();
            gameObject.SetActive(false);
        }
    }
}
