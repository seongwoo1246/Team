using UnityEngine;

public class LoginView : MonoBehaviour
{
    [Header("로그인 메인 패널")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private UnityEngine.UI.Button googleLoginButton;
    [SerializeField] private UnityEngine.UI.Button emailPopupButton;

    public event System.Action OnGoogleLoginClicked;
    public event System.Action OnEmailLoginClicked;

    private void Awake()
    {
        if (googleLoginButton != null)
            googleLoginButton.onClick.AddListener(() => OnGoogleLoginClicked?.Invoke());

        if (emailPopupButton != null)
            emailPopupButton.onClick.AddListener(() => OnEmailLoginClicked?.Invoke());
    }

    public void SetPanelActive(bool active)
    {
        if(loginPanel != null) loginPanel.SetActive(active);
    }
}
