using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ErrorPopupUI : MonoBehaviour
{
    [SerializeField] private TMP_Text errorMessageText;
    [SerializeField] private Button retryButton;

    /// <summary>
    /// 에러 메시지를 표시하고 '다시 시도' 버튼을 누를 때까지 대기
    /// </summary>
    public async UniTask ShowAndReTryAsync(string message, CancellationToken ct)
    {
        errorMessageText.text = message;
        gameObject.SetActive(true);

        // 다시 시도 버튼을 누를 때까지 대기
        await retryButton.OnClickAsync(ct);
        gameObject.SetActive(false);
    }
}
