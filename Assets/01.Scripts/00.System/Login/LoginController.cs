/* 담담자 - 송태훈
 
 */

using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UtilDebug = DebugLogger<LoginController>;

public class LoginController : MonoBehaviour
{
    private const string PREFS_LOCAL_GUEST_ACTIVE = "IS_LOCAL_GUEST_ACTIVE";

    [Header("로그인 메인 View")]
    [SerializeField] private LoginView loginView;

    [Header("로그인 UI 팝업")]
    [SerializeField] private EmailLogin emailLoginPopupUI;
    [SerializeField] private NicknamePopupUI nicknamePopupUI;
    [SerializeField] private LoadingStatusPopupUI loadingPopupUI;

    [Header("Auth Google")]
    [SerializeField] private GoogleLogin googleLogin;

    private UniTaskCompletionSource<bool> loginCompletionSource;
    private bool isFlowActive = false;

    private void Awake()
    {
        if (loginView != null)
        {
            if (googleLogin != null)
                loginView.OnGoogleLoginClicked += () => HandleGoogleLoginClicked();
            loginView.OnEmailLoginClicked += () => HandleEmailLoginClicked();

            // 로컬 게스트 로그인 버튼 이벤트 바인딩
            loginView.OnLocalGuestLoginClicked += () => HandleLocalGuestLoginClicked();
        }

        if (nicknamePopupUI != null)
            nicknamePopupUI.OnNicknameConfirmed += OnNicknameSubmitted;

        if (googleLogin != null)
            googleLogin.OnLogStatus += OnGoogleLoginStatusChanged;

        if (emailLoginPopupUI != null)
            emailLoginPopupUI.OnStatusChanged += OnAuthStatusChanged;

        SetAllUIActive(false);
    }

    private void OnDestroy()
    {
        if (loginView != null)
        {
            if (googleLogin != null)
                loginView.OnGoogleLoginClicked -= () => HandleGoogleLoginClicked();
            loginView.OnEmailLoginClicked -= () => HandleEmailLoginClicked();

            loginView.OnLocalGuestLoginClicked -= () => HandleLocalGuestLoginClicked();
        }

        if (nicknamePopupUI != null)
            nicknamePopupUI.OnNicknameConfirmed -= OnNicknameSubmitted;

        if (googleLogin != null)
            googleLogin.OnLogStatus -= OnGoogleLoginStatusChanged;

        if (emailLoginPopupUI != null)
            emailLoginPopupUI.OnStatusChanged -= OnAuthStatusChanged;
    }
    private void HandleGoogleLoginClicked() => googleLogin?.RequestGoogleLogin();
    private void HandleEmailLoginClicked()=>  emailLoginPopupUI?.OpenPopup();

    /// <summary>
    /// 로컬 게스트 로그인 버튼 클릭 시 호출
    /// </summary>
    private void HandleLocalGuestLoginClicked()
    {
        UtilDebug.Log("로컬 게스트 로그인 시작");
        UserManager.Instance.IsLocalMode = true;

        // 3. 로컬 데이터가 이미 존재하는지 확인
        if (UserManager.Instance.HasLocalSaveData())
        {
            // 기존 세이브가 있으면 바로 계정 정보 로드 및 로비 진입 진행
            PlayerPrefs.SetInt(PREFS_LOCAL_GUEST_ACTIVE, 1);
            PlayerPrefs.Save();
            ProcessUserVerificationAsync(UserManager.Instance.LocalGuestUID, this.GetCancellationTokenOnDestroy()).Forget();
        }
        else
        {
            // 신규 게스트 유저: 닉네임 입력 팝업 직접 오픈
            UtilDebug.Log("[LoginController] 신규 게스트 계정 -> 닉네임 팝업 오픈");
            nicknamePopupUI?.Open();
        }
    }

    /// <summary>
    /// BootstrapController에서 초훌. 로그인 완료(인증 및 유저 데이터 검증)까지 대기
    /// </summary>
    public async UniTask<bool> LoginFlowAsync(CancellationToken ct)
    {
        loginCompletionSource = new UniTaskCompletionSource<bool>();
        isFlowActive = true;

        // LoginFlow가 실행되는 동안에만 인증 상태 변화 이벤트 구독
        AuthLoginSystem.Instance.OnAuthStateChanged += HandleAuthStateChanged;

        try
        {
            // 1. 로컬 게스트로 플레이하던 유저인지 체크 (재접속 자동 로그인)
            if (PlayerPrefs.GetInt("IS_LOCAL_GUEST_ACTIVE", 0) == 1 && UserManager.Instance.HasLocalSaveData())
            {
                UtilDebug.Log("이전 로컬 게스트 세션 감지: 로컬 자동 로그인 진행");
                UserManager.Instance.IsLocalMode = true;
                ProcessUserVerificationAsync(UserManager.Instance.LocalGuestUID, ct).Forget();
            }

            // 2. Firebase 기존 로그인 세션이 남아있는지 확인 ( 자동 로그인 검사 )
            else if (AuthLoginSystem.Instance.CurrentUser != null)
            {
                UtilDebug.Log("기존 세션 감지 : 자동 로그인 진행");
                ProcessUserVerificationAsync(AuthLoginSystem.Instance.UserId, ct).Forget();
            }
            else
            {
                // 로그인 세션이 없으면 로그인 메인 패널 노출
                loginView.SetPanelActive(true);
            }

            return await loginCompletionSource.Task.AttachExternalCancellation(ct);
        }
        finally
        {
            isFlowActive = false;
            if (AuthLoginSystem.Instance != null)
            {
                AuthLoginSystem.Instance.OnAuthStateChanged -= HandleAuthStateChanged;
            }
            SetAllUIActive(false);
        }
    }
    #region Auth 이벤트 처리
    private void HandleAuthStateChanged(bool isLoggedIn, string message)
    {
        if (!isFlowActive) return;

        // 로컬 모드 진행 중에는 Firebase Auth 변경 무시
        if (UserManager.Instance.IsLocalMode) return;

        if (!isLoggedIn)
        {
            UserManager.Instance.ClearLocalData();
            loadingPopupUI?.ForceHide();
            loginView?.SetPanelActive(true);
            return;
        }

        // 로그인 상태로 바뀌었을 때 검증 비동기 실행
        ProcessUserVerificationAsync(AuthLoginSystem.Instance.UserId, this.GetCancellationTokenOnDestroy()).Forget();
    }


    private async UniTaskVoid ProcessUserVerificationAsync(string uid, CancellationToken ct)
    {
        loadingPopupUI?.ShowLoading("유저 계정 정보 확인 중...");
        var (exists, data) = await UserManager.Instance.LoadUserInfoAsync(uid, ct);
        if (exists)
        {
            loadingPopupUI?.ForceHide();
            loginCompletionSource?.TrySetResult(true);
        }
        else
        {
            loginView?.SetPanelActive(false);
            loadingPopupUI?.ForceHide();
            nicknamePopupUI?.Open();
        }
    }
    private void OnNicknameSubmitted(string nickname)
    {
        CreateAccountAsync(nickname).Forget();
    }

    private async UniTaskVoid CreateAccountAsync(string nickname)
    {
        var ct = this.GetCancellationTokenOnDestroy();
        string uid = AuthLoginSystem.Instance.UserId;

        loadingPopupUI?.ShowLoading($"계정 생성 중 {nickname}");

        bool isDuplicate = await UserManager.Instance.IsNicknameDuplicateAsync(nickname, ct);
        if (isDuplicate)
        {
            await loadingPopupUI.HideAsync();
            nicknamePopupUI.ShowDuplication("이미 사용 중인 닉네임");
            return;
        }

        bool success = await UserManager.Instance.CreateUserInfoAsync(uid, nickname, ct);
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
        loginView?.SetPanelActive(active);
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
