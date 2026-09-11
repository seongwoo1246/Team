/* 담담자 - 송태훈
 
 */

using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UtilDebug = DebugLogger<LoginController>;

public class LoginController : MonoBehaviour
{
    [Header("로그인 메인 패널")]
    [SerializeField] private GameObject loginPanel;

    [Header("로그인 UI 팝업")]
    [SerializeField] private EmailLogin emailLoginPopupUI;
    [SerializeField] private NicknamePopupUI nicknamePopupUI;
    [SerializeField] private LoadingStatusPopupUI loadingPopupUI;

    [Header("로그인 메인 버튼")]
    [SerializeField] private Button googleLoginButton;
    [SerializeField] private Button emailPopupButton;

    [Header("Auth Google")]
    [SerializeField] private GoogleLogin googleLogin;

    private UniTaskCompletionSource<bool> loginCompletionSource;
    private bool isFlowActive = false;

    private void Awake()
    {
        if(googleLoginButton != null && googleLogin != null)
            googleLoginButton.onClick.AddListener(googleLogin.RequestGoogleLogin);

        if(emailPopupButton != null && emailLoginPopupUI != null)
            emailPopupButton.onClick.AddListener(emailLoginPopupUI.OpenPopup);

        if (nicknamePopupUI != null)
            nicknamePopupUI.OnNicknameConfirmed += OnNicknameSubmitted;

        if (googleLogin != null)
            googleLogin.OnLogStatus += OnGoogleLoginStatusChanged;

        if(emailLoginPopupUI != null)
            emailLoginPopupUI.OnStatusChanged += OnAuthStatusChanged;

        SetAllUIActive(false);
    }
    
    private void OnDestroy()
    {
        if (googleLoginButton != null)
            googleLoginButton.onClick.RemoveListener(googleLogin.RequestGoogleLogin);

        if (emailPopupButton != null)
            emailPopupButton.onClick.RemoveListener(emailLoginPopupUI.OpenPopup);

        if (nicknamePopupUI != null)
            nicknamePopupUI.OnNicknameConfirmed -= OnNicknameSubmitted;

        if (googleLogin != null)
            googleLogin.OnLogStatus -= OnGoogleLoginStatusChanged;

        if (emailLoginPopupUI != null)
            emailLoginPopupUI.OnStatusChanged -= OnAuthStatusChanged;
    }

    /// <summary>
    /// BootstrapController에서 초훌. 로그인 완료(인증 및 유저 데이터 검증)까지 대기
    /// </summary>
    public async UniTask<bool> LoginFlowAsync(CancellationToken ct)
    {
        loginCompletionSource = new UniTaskCompletionSource<bool>();
        isFlowActive = true;

        // LoginFlow가 실행되는 동안에만 인증 상태 변화 이벤트 구독
        AuthLoginSystem.instance.OnAuthStateChanged += HandleAuthStateChanged;

        try
        {
            // 1. 기존 로그인 세션이 남아있는지 확인 ( 자동 로그인 검사 )
            if (AuthLoginSystem.instance.CurrentUser != null)
            {
                UtilDebug.Log("기존 세션 감지 : 자동 로그인 진행");
                ProcessUserVerificationAsync(AuthLoginSystem.instance.UserId, ct).Forget();
            }
            else
            {
                // 로그인 세션이 없으면 로그인 버튼 활성화
                if (loginPanel != null) loginPanel.SetActive(true);
            }

            bool result = await loginCompletionSource.Task.AttachExternalCancellation(ct);
            return result;
        }
        finally
        {
            isFlowActive = false;
            if (AuthLoginSystem.instance != null)
            {
                AuthLoginSystem.instance.OnAuthStateChanged -= HandleAuthStateChanged;
            }
            SetAllUIActive(false);
        }
    }
    #region Auth 이벤트 처리
    private void HandleAuthStateChanged(bool isLoggedIn, string message)
    {
        if (!isFlowActive) return;

        if(!isLoggedIn)
        {
            UserManager.instance.ClearLocalData();
            loadingPopupUI?.ForceHide();
            if(loginPanel !=null) loginPanel.SetActive(true);
            return;
        }
        
        // 로그인 상태로 바뀌었을 때 검증 비동기 시ㅣㄹ행
        ProcessUserVerificationAsync(AuthLoginSystem.instance.UserId, this.GetCancellationTokenOnDestroy()).Forget();
    }


    private async UniTaskVoid ProcessUserVerificationAsync(string uid, CancellationToken ct)
    {
        loadingPopupUI?.ShowLoading("유저 계정 정보 확인 중...");

        var (exists, data) = await UserManager.instance.LoadUserInfoAsync(uid, ct);
        if (exists)
        {
            loadingPopupUI?.ForceHide();
            loginCompletionSource?.TrySetResult(true);
        }
        else
        {
            if (loginPanel != null) loginPanel.SetActive(false);
            loadingPopupUI?.ForceHide();
            nicknamePopupUI.Open();
        }
    }
    private void OnNicknameSubmitted(string nickname)
    {
        CreateAccountAsync(nickname).Forget();
    }

    private async UniTaskVoid CreateAccountAsync(string nickname)
    {
        var ct = this.GetCancellationTokenOnDestroy();
        string uid = AuthLoginSystem.instance.UserId;

        loadingPopupUI?.ShowLoading($"계정 생성 중 {nickname}");

        bool isDuplicate = await UserManager.instance.IsNicknameDuplicateAsync(nickname, ct);
        if (isDuplicate)
        {
            await loadingPopupUI.HideAsync();
            nicknamePopupUI.ShowDuplication("이미 사용 중인 닉네임");
            return;
        }

        bool success = await UserManager.instance.CreateUserInfoAsync(uid, nickname, ct);
        if (success)
        {
            nicknamePopupUI.Close();
            loadingPopupUI?.ForceHide();
            // 신규 가입 완료 -> 로그인 성공 리턴
            loginCompletionSource?.TrySetResult(true);
        }
        else
        {
            await loadingPopupUI.ShowMessageAndHideAsync("계정 생성에 실패");
        }
    }
    #endregion

    private void SetAllUIActive(bool active)
    {
        if (loginPanel != null) loginPanel.SetActive(active);
        if (emailLoginPopupUI != null) emailLoginPopupUI.ClosePopup();
        if (nicknamePopupUI != null) nicknamePopupUI.Close();
        if (loadingPopupUI != null) loadingPopupUI.ForceHide();
    }

    private void OnGoogleLoginStatusChanged(string message) => UtilDebug.Log($"[GoogleLogin] {message}");
    private void OnAuthStatusChanged(string message)
    {
        UtilDebug.Log($"[Auth] {message}");
        loadingPopupUI?.UpdateMessage(message);
    }
}
