using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EmailLoginTest : MonoBehaviour
{
    [Header("UI 바인딩")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        // 람다 없이 메서드 바인딩 (GC Alloc 방지)
        loginButton.onClick.AddListener(OnLoginClick);
        registerButton.onClick.AddListener(OnRegisterClick);
        closeButton.onClick.AddListener(ClosePopup);
    }

    private void OnDestroy()
    {
        loginButton.onClick.RemoveListener(OnLoginClick);
        registerButton.onClick.RemoveListener(OnRegisterClick);
        closeButton.onClick.RemoveListener(ClosePopup);
    }

    public void OpenPopup()
    {
        emailInput.text = string.Empty;
        passwordInput.text = string.Empty;
        gameObject.SetActive(true);
    }

    public void ClosePopup()
    {
        gameObject.SetActive(false);
    }

    private void OnLoginClick()
    {
        ExecuteEmailLoginAsync().Forget();
    }

    private void OnRegisterClick()
    {
        ExecuteEmailRegisterAsync().Forget();
    }

    private async UniTaskVoid ExecuteEmailLoginAsync()
    {
        string email = emailInput.text.Trim();
        string pw = passwordInput.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pw))
            return;

        bool success = await LoginSystemTest.instance.SignInWithEmailAsync(email, pw, this.GetCancellationTokenOnDestroy());
        if (success)
        {
            ClosePopup();
        }
    }

    private async UniTaskVoid ExecuteEmailRegisterAsync()
    {
        string email = emailInput.text.Trim();
        string pw = passwordInput.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pw))
            return;

        bool success = await LoginSystemTest.instance.CreateWithEmailAsync(email, pw, this.GetCancellationTokenOnDestroy());
        if (success)
        {
            ClosePopup();
        }
    }
}