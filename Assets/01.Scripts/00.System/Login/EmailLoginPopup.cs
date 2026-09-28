/*담담자 - 송태훈
이메일 및 비밀번호 입력을 통한 로그인과 회원가입 요청을 처리하는 팝업 UI
입력값 유효성 검사, 상태 알림 이벤트 호출 및 AuthLoginSystem과의 통신 결과를 제어
 */
using Cysharp.Threading.Tasks;
using UnityEngine;
using UtilDebug = DebugLogger<EmailLoginPopup>;

public class EmailLoginPopup : MonoBehaviour
{
    [Header("UI 바인딩")]
    [SerializeField] private TMPro.TMP_InputField emailInput;
    [SerializeField] private TMPro.TMP_InputField passwordInput;
    [SerializeField] private TMPro.TMP_Text errorText;
    [SerializeField] private UnityEngine.UI.Button loginButton;
    [SerializeField] private UnityEngine.UI.Button registerButton;
    [SerializeField] private UnityEngine.UI.Button closeButton;


    public event System.Action<string> OnStatusChanged;
    public event System.Action<string> OnError;
    private void OnEnable()
    {
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
            UtilDebug.LogWarning("이메일 또는 비밀번호를 입력해주세요.");
            OnStatusChanged?.Invoke("이메일/비밀번호를 입력해주세요.");
            OnError?.Invoke("이메일/비밀번호를 입력해주세요.");
            return;
        }

        OnStatusChanged?.Invoke("이메일 로그인 중..");

        bool success = await AuthLoginSystem.Instance.SignInWithEmailAsync(email, pw, this.GetCancellationTokenOnDestroy());
        if (success)
        {
            ClosePopup();
        }
        else
        {
            UtilDebug.LogWarning("로그인 실패: 이메일 또는 비밀번호 확인");
            OnStatusChanged?.Invoke("로그인 실패: 이메일 또는 비밀번호 확인");
            OnError?.Invoke("이메일 또는 비밀번호 확인");
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
            UtilDebug.LogWarning("이메일 또는 비밀번호를 입력해주세요.");
            OnStatusChanged?.Invoke("이메일/비밀번호를 입력해주세요.");
            OnError?.Invoke("이메일/비밀번호를 입력해주세요."); 
            return;
        }

        bool success = await AuthLoginSystem.Instance.CreateWithEmailAsync(email, pw, this.GetCancellationTokenOnDestroy());
        if (success)
        {
            ClosePopup();
        }
        else
        {
            UtilDebug.LogWarning("회원가입 실패: 이미 존재하는 계정이거나 규칙에 맞지 않습니다.");
            OnStatusChanged?.Invoke("회원가입 실패: 이미 존재하는 계정이거나 규칙에 맞지 않습니다.");
            OnError?.Invoke("회원가입 실패: 이미 존재하는 계정이거나 규칙에 맞지 않습니다.");
        }
    }
    private void OnErrorText(string message) => errorText.text = message;
}