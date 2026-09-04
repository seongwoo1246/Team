/*
 LoginSystemTest에 내장된 Firebase 메서드를 통해 이메일로 회원가입 및 로그인 기능을 테스트하는 스크립트.
 */
using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EmailLogin : MonoBehaviour
{
    [Header("UI 바인딩")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button closeButton;


    // 
    public event Action<string> OnStatusChanged;
    public event Action<string> OnError;
    private void OnEnable()
    {
        // 람다 없이 메서드 바인딩 (GC Alloc 방지)
        loginButton.onClick.AddListener(OnLoginClick);
        registerButton.onClick.AddListener(OnRegisterClick);
        closeButton.onClick.AddListener(ClosePopup);
        OnError += OnErrorText;
    }

    private void OnDisable()
    {
        loginButton.onClick.RemoveListener(OnLoginClick);
        registerButton.onClick.RemoveListener(OnRegisterClick);
        closeButton.onClick.RemoveListener(ClosePopup);
        OnError -= OnErrorText;
    }

    public void OpenPopup()
    {
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

    /// <summary>
    /// 이메일 계정과 비밀번호를 사용하여 로그인 시도
    /// </summary>
    private async UniTaskVoid ExecuteEmailLoginAsync()
    {
        string email = emailInput.text.Trim();
        string pw = passwordInput.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pw))
        {
            Debug.LogWarning("이메일 또는 비밀번호를 입력해주세요.");
            OnStatusChanged?.Invoke("이메일/비밀번호를 입력해주세요.");
            OnError?.Invoke("이메일/비밀번호를 입력해주세요.");
            return;
        }

        OnStatusChanged?.Invoke("이메일 로그인 중..");

        bool success = await AuthLoginSystem.instance.SignInWithEmailAsync(email, pw, this.GetCancellationTokenOnDestroy());
        if (success)
        {
            ClosePopup();
        }
        else
        {
            Debug.LogWarning("로그인 실패: 이메일 또는 비밀번호 확인");
            OnStatusChanged?.Invoke("로그인 실패: 이메일 또는 비밀번호 확인");
            OnError?.Invoke("이메일/비밀번호를 입력해주세요.");
        }
    }

    /// <summary>
    /// 이메일 계정과 비밀번호를 사용하여 회원가입 시도
    /// </summary>
    private async UniTaskVoid ExecuteEmailRegisterAsync()
    {
        string email = emailInput.text.Trim();
        string pw = passwordInput.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pw))
        {
            Debug.LogWarning("이메일 또는 비밀번호를 입력해주세요.");
            OnStatusChanged?.Invoke("이메일/비밀번호를 입력해주세요.");
            OnError?.Invoke("이메일/비밀번호를 입력해주세요."); 
            return;
        }

        bool success = await AuthLoginSystem.instance.CreateWithEmailAsync(email, pw, this.GetCancellationTokenOnDestroy());
        if (success)
        {
            ClosePopup();
        }
        else
        {
            Debug.LogWarning("회원가입 실패: 이미 존재하는 계정이거나 규칙에 맞지 않습니다.");
            OnStatusChanged?.Invoke("회원가입 실패: 이미 존재하는 계정이거나 규칙에 맞지 않습니다.");
            OnError?.Invoke("회원가입 실패: 이미 존재하는 계정이거나 규칙에 맞지 않습니다.");
        }
    }
    private void OnErrorText(string message) => errorText.text = message;
}