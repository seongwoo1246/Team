/*담담자 - 송태훈
신규 유저의 닉네임 입력 및 유효성(최소 길이 등) 검사를 처리하는 팝업 UI
중복 검사 실패 메시지를 표출하고, 확정된 닉네임을 상위 컨트롤러에 이벤트로 전달
 */

using UnityEngine;
using UtilDebug = DebugLogger<NicknamePopupUI>;

public class NicknamePopupUI : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_InputField nicknameInput;
    [SerializeField] private UnityEngine.UI.Button confirmButton;
    [SerializeField] private TMPro.TMP_Text duplicaiotnCheck;

    // LoginController에서 참조
    public event System.Action<string> OnNicknameConfirmed;

    private void OnEnable()
    {
        confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    private void OnDisable()
    {
        confirmButton.onClick.RemoveListener(OnConfirmClicked);
    }

    public void Open()
    {
        nicknameInput.text = string.Empty;
        duplicaiotnCheck.text = string.Empty;
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void OnConfirmClicked()
    {
        string nick = nicknameInput.text.Trim();
        if (string.IsNullOrEmpty(nick) || nick.Length < 2)
        {
            return;
        }
        UtilDebug.Log($"닉네임 입력 완료: {nick}, 클릭");

        OnNicknameConfirmed?.Invoke(nick);
    }

    public void ShowDuplication(string duplication)
    {
        duplicaiotnCheck.text = duplication;
        UtilDebug.Log($"닉네임 중복 {duplication}");
    }
}
