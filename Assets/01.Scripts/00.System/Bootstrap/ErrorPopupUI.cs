/* 담담자 - 송태훈
부트스트랩 단계 중 발생한 에러 메시지를 표시하고 유저 입력을 대기
 */
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ErrorPopupUI : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_Text errorMessageText;
    [SerializeField] private UnityEngine.UI.Button retryButton;

    /// <summary>
    /// 에러 메시지를 표시하고 '다시 시도' 버튼을 누를 때까지 대기
    /// </summary>
    public async UniTask ShowAndReTryAsync(string message, System.Threading.CancellationToken ct)
    {
        errorMessageText.text = message;
        gameObject.SetActive(true);

        // 다시 시도 버튼을 누를 때까지 대기
        await retryButton.OnClickAsync(ct);
        gameObject.SetActive(false);
    }
}
