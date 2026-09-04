using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UtilDebug = DebugLogger<LoginController>;

public class LoginController : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private EmailLogin emailLoginPopupUI;
    [SerializeField] private NicknamePopupUI nicknamePopupUI;
    [SerializeField] private LoadingStatusPopupUI loadingPopupUI;

    [Header("메인 UI")]
    [SerializeField] private Button googleLoginButton;
    [SerializeField] private Button emailPopupButton;

    [Header("Auth Google")]
    [SerializeField] private GoogleLogin googleLogin;


    private const string LOBBY_SCENE = "LobbySceneTest(Server)";

    private void Awake()
    {
        if(googleLoginButton != null)
            googleLoginButton.onClick.AddListener(googleLogin.RequestGoogleLogin);

        if(emailPopupButton != null)
            emailPopupButton.onClick.AddListener(emailLoginPopupUI.OpenPopup);

        if (nicknamePopupUI != null)
            nicknamePopupUI.OnNicknameConfirmed += OnNicknameSubmitted;

        if (googleLogin != null)
            googleLogin.OnLogStatus += OnGoogleLoginStatusChanged;


        if(emailLoginPopupUI != null)
            emailLoginPopupUI.OnStatusChanged += OnAuthStatusChanged;
    }
    private void Start()
    {
        if (loadingPopupUI != null) 
            loadingPopupUI.ForceHide();

        if (emailLoginPopupUI != null) 
            emailLoginPopupUI.ClosePopup();

        loadingPopupUI?.ShowLoading("서버 연결 초기화 중..");
        UserDataManager.instance.Init();
        AuthLoginSystem.instance.Init();

        AuthLoginSystem.instance.OnAuthStateChanged += HandleAuthStateChanged;
        // 추후 AsyncSceneManger를 통해서 비동기 치러로 변경해야함
        loadingPopupUI?.HideAsync();
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

        if (AuthLoginSystem.instance != null)
            AuthLoginSystem.instance.OnAuthStateChanged -= HandleAuthStateChanged;
    }

    #region UI 이벤트
    private void OnGoogleLoginStatusChanged(string message)
    {
        UtilDebug.Log($"[GoogleLogin] {message}");
    }
    private void OnAuthStatusChanged(string message)
    {
        UtilDebug.Log($"[Auth] {message}");
        loadingPopupUI?.UpdateMessage(message);
    }

    private void OnNicknameSubmitted(string nickname)
    {
        CreateAccountAndEnterGameAsync(nickname).Forget();
    }
    #endregion

    #region Auth 로직
    private void HandleAuthStateChanged(bool isLoggedIn, string msg)
    {
        if (!isLoggedIn)
        {
            UserDataManager.instance.ClearLocalData();
            loadingPopupUI?.ForceHide();
            return;
        }
        ProcessLoginFlowAsync(AuthLoginSystem.instance.UserId).Forget();
    }

    /// <summary>
    /// 로그인 완류 후 유저 데이터 존재 여부를 확인하고, 존재하면 로비 씬으로 이동, 존재하지 않으면 닉네임 입력 팝업을 띄움
    /// </summary>
    private async UniTaskVoid ProcessLoginFlowAsync(string uid)
    {
        var ct = this.GetCancellationTokenOnDestroy();
        loadingPopupUI?.ShowLoading("유저 계정 정보 확인 중...");

        var (exists, data) = await UserDataManager.instance.LoadUserDataAsync(uid, ct);
        if (exists)
        {
            loadingPopupUI?.ShowLoading("유저 계정 정보 확인 중...");
            EnterLobby();
        }
        else
        {
            loadingPopupUI?.ForceHide();
            UtilDebug.Log("신규 유저 - 계정 생성");
            nicknamePopupUI.Open();
        }
    }

    /// <summary>
    /// 신규 유저 데이터 생성 후 로비 진입
    /// </summary>
    private async UniTaskVoid CreateAccountAndEnterGameAsync(string nickname)
    {
        var ct = this.GetCancellationTokenOnDestroy();
        string uid = AuthLoginSystem.instance.UserId;

        loadingPopupUI?.ShowLoading($"계정 생성 중.. {nickname}");
        bool isDuplicate = await UserDataManager.instance.IsNicknameDuplicateAsync(nickname);
        if(isDuplicate)
        {
            if(loadingPopupUI != null)
            { await loadingPopupUI.HideAsync(); }

            nicknamePopupUI.ShowDuplication("이미 사용 중인 닉네임");
        }


        bool success = await UserDataManager.instance.CreateUserDataAsync(uid, nickname, ct);
        if (success)
        {
            UtilDebug.Log("계정 생성 성공");
            nicknamePopupUI.Close();
            loadingPopupUI?.ShowLoading("로비로 이동");
            EnterLobby();
        }
        else
        {
            UtilDebug.LogError("계정 생성 실패");
            await loadingPopupUI.ShowMessageAndHideAsync("계정 생성에 실패. 다시");
        }
    }

    #endregion

    private void EnterLobby()
    {
        // 로드 씬 비동기적 처리를 해야할 필요가 있음
        ScenesManager.instance.StringToLoadScecn(LOBBY_SCENE);
    }
}
