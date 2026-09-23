// 담당자 - 송태훈
using UnityEngine;

public class LoginView : MonoBehaviour
{
    [Header("로그인 메인 패널")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private UnityEngine.UI.Button googleLoginButton;
    [SerializeField] private UnityEngine.UI.Button emailPopupButton;
    [SerializeField] private UnityEngine.UI.Button localGuestLoginButton;


    public event System.Action OnGoogleLoginClicked;
    public event System.Action OnEmailLoginClicked;
    public event System.Action OnLocalGuestLoginClicked;


    private void Awake()
    {
        if (googleLoginButton != null)
            googleLoginButton.onClick.AddListener(() => OnGoogleLoginClicked?.Invoke());

        if (emailPopupButton != null)
            emailPopupButton.onClick.AddListener(() => OnEmailLoginClicked?.Invoke());
        if (localGuestLoginButton != null)
        {
            localGuestLoginButton.onClick.AddListener(() => OnLocalGuestLoginClicked?.Invoke());
        }
    }

    public void SetPanelActive(bool active)
    {
        if(loginPanel != null) loginPanel.SetActive(active);
    }
}
