using Cysharp.Threading.Tasks;
using UnityEngine;

public class LoginControllerTest : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private NicknamePopupUI nicknamePopupUI;

    private const string LOBBY_SCENE = "LobbySceneTest(Server)";

    private void Awake()
    {
        if (nicknamePopupUI != null)
        {
            nicknamePopupUI.Close();
            nicknamePopupUI.OnNicknameConfirmed += OnNickNameSubmitted;
        }
    }
    private void Start()
    {
        UserDataManager.instance.Init();
        LoginSystemTest.instance.OnAuthStateChanged += HandleAuthStateChanged;
    }
    private void OnDestroy()
    {
        if (nicknamePopupUI != null)
            nicknamePopupUI.OnNicknameConfirmed -= OnNickNameSubmitted;

        if (LoginSystemTest.instance != null)
            LoginSystemTest.instance.OnAuthStateChanged -= HandleAuthStateChanged;
    }

    private void HandleAuthStateChanged(bool isLoggedIn, string msg)
    {
        if (!isLoggedIn)
        {
            UserDataManager.instance.ClearLocalData();
            return;
        }
        ProcessLoginFlowAsync(LoginSystemTest.instance.UserId).Forget();
    }

    private async UniTaskVoid ProcessLoginFlowAsync(string uid)
    {
        var ct = this.GetCancellationTokenOnDestroy();

        var (exists, data) = await UserDataManager.instance.LoadUserDataAsync(uid, ct);
        if (exists)
        {
            // 로드 씬 비동기적 처리를 해야할 필요가 있음
            ScenesManager.instance.StringToLoadScecn(LOBBY_SCENE);
        }
        else
        {
            nicknamePopupUI.Open();
        }
    }

    private void OnNickNameSubmitted(string nickname)
    {
        CreateAccountAndEnterGameAsync(nickname).Forget();
    }

    private async UniTaskVoid CreateAccountAndEnterGameAsync(string nickname)
    {
        var ct = this.GetCancellationTokenOnDestroy();
        string uid = LoginSystemTest.instance.UserId;

        bool success = await UserDataManager.instance.CreateUserDataAsync(uid, nickname, ct);
        if(success)
        {
            nicknamePopupUI.Close();
            ScenesManager.instance.StringToLoadScecn(LOBBY_SCENE);
        }
    }

}
