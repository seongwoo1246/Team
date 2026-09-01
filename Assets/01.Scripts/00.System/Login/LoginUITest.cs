using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginUITest : MonoBehaviour
{
    [Header("참조 컴포넌트")]
    [SerializeField] private EmailLoginTest emailLogin;
    [SerializeField] private GoogleLoginTest googleLogin;

    [Header("메인 UI")]
    [SerializeField] private Button emailPopupButton;
    [SerializeField] private Button googleLoginButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Awake()
    {
        // 람다 제거 바인딩
        emailPopupButton.onClick.AddListener(emailLogin.OpenPopup);
        googleLoginButton.onClick.AddListener(googleLogin.RequestGoogleLogin);
        logoutButton.onClick.AddListener(OnLogoutClicked);
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
    }

    private void OnLogoutClicked()
    {
        LoginSystemTest.instance.SignOut();
    }

    private void UpdateAuthUI(bool isLoggedIn, string message)
    {
        UpdateStatusText(isLoggedIn ? $"로그인 완료: {message}" : "로그아웃 상태");
        logoutButton.gameObject.SetActive(isLoggedIn);
        emailPopupButton.gameObject.SetActive(!isLoggedIn);
        googleLoginButton.gameObject.SetActive(!isLoggedIn);
    }

    private void UpdateStatusText(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}