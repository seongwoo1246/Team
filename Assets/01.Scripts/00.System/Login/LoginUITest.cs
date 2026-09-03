using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = DebugLogger<LoginUITest>;

public class LoginUITest : MonoBehaviour
{
    [Header("참조 컴포넌트")]
    [SerializeField] private EmailLoginTest emailLogin;
    [SerializeField] private GoogleLoginTest googleLogin;

    [Header("메인 UI")]
    [SerializeField] private Button emailPopupButton;
    [SerializeField] private Button googleLoginButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button deleteAccountButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Awake()
    {
        emailPopupButton.onClick.AddListener(emailLogin.OpenPopup);
        googleLoginButton.onClick.AddListener(googleLogin.RequestGoogleLogin);
        logoutButton.onClick.AddListener(OnLogoutClicked);

        if (deleteAccountButton != null)
            deleteAccountButton.onClick.AddListener(OnDeleteAccountClicked);
    }

    private void Start()
    {
        LoginSystemTest.instance.Init();
        LoginSystemTest.instance.OnAuthStateChanged += UpdateAuthUI;
        googleLogin.OnLogStatus += UpdateStatusText;

        emailLogin.ClosePopup();
        UpdateStatusText("로그인 대기 중");
    }

    private void OnDestroy()
    {
        // 리스너 및 이벤트 해제 (메모리 릭 방지)
        emailPopupButton.onClick.RemoveListener(emailLogin.OpenPopup);
        googleLoginButton.onClick.RemoveListener(googleLogin.RequestGoogleLogin);
        logoutButton.onClick.RemoveListener(OnLogoutClicked);

        if (LoginSystemTest.instance != null)
            LoginSystemTest.instance.OnAuthStateChanged -= UpdateAuthUI;

        if (googleLogin != null)
            googleLogin.OnLogStatus -= UpdateStatusText;

        if(deleteAccountButton != null)
            deleteAccountButton.onClick.RemoveListener(OnDeleteAccountClicked);
    }

    private void OnLogoutClicked()
    {
        LoginSystemTest.instance.SignOut();
    }

    private void OnDeleteAccountClicked()
    {
        ExcuteDeleteAccountAsync().Forget();
    }

    /// <summary>
    /// Firebase 인증 상태 변경 시 UI를 업데이트하는 메서드. 로그인 상태에 따라 버튼 활성화/비활성화 및 상태 텍스트를 변경하도록 되어 있으나 추후 진행에 따라 삭제
    /// </summary>
    /// <param name="isLoggedIn"></param>
    /// <param name="message"></param>
    private void UpdateAuthUI(bool isLoggedIn, string message)
    {
        UpdateStatusText(isLoggedIn ? $"로그인 완료: {message}" : "로그아웃 상태");
        logoutButton.gameObject.SetActive(isLoggedIn);
        emailPopupButton.gameObject.SetActive(!isLoggedIn);
        googleLoginButton.gameObject.SetActive(!isLoggedIn);
    }

    /// <summary>
    /// Firebase 서버에서 등록된 계정을 삭제하는 비동기 메서드. 추후 LogUI가 아닌 메인 로비 씬에서 계정 삭제를 진행하도록 변경 필요.
    /// </summary>
    /// <returns></returns>
    private async UniTaskVoid ExcuteDeleteAccountAsync()
    {
        var (success, erroMsg) = await LoginSystemTest.instance.DeleteAccountAsync(this.GetCancellationTokenOnDestroy());
        if (success)
        {
            UpdateStatusText("계정 삭제 완료");
        }
        else
        {
            UpdateStatusText("계정 삭제 실패");
        }
    }

    private void UpdateStatusText(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}