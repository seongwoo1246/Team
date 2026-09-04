/*
Firebase를 이용한 로그인 시스템 테스트용 클래스입니다.
Email / Password 로그인, Google 로그인, 계정 삭제 기능을 포함하고 있으며, 인증 상태 변경 이벤트를 통해 UI 업데이트를 지원합니다.
 */
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UtilDebug = DebugLogger<AuthLoginSystem>;

public class AuthLoginSystem : NonMonoSingleton<AuthLoginSystem>
{
    private FirebaseAuth auth;
    private FirebaseUser user;

    public FirebaseUser CurrentUser => user;
    public string UserId => user != null ? user.UserId : string.Empty;

    public event Action<bool, string> OnAuthStateChanged;

    public override void Init()
    {
        base.Init();
        InitializeFirebaseAsync().Forget();
    }

    /// <summary>
    /// Firebase 초기화 및 종속성 확인 후 FirebaseAuth 인스턴스를 가져옵니다.
    /// </summary>
    private async UniTaskVoid InitializeFirebaseAsync()
    {
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync().AsUniTask();
        if (dependencyStatus == DependencyStatus.Available)
        {
            auth = FirebaseAuth.DefaultInstance;
            // static으로 인한 user 메모리 저장을 해제하는 임시 처리. 테스트를 위해서
            if (auth.CurrentUser != null)
            {
                SignOut();
            }
            auth.StateChanged += HandleAuthStateChanged;
        }
        else
        {
            UtilDebug.LogError($"Firebase 종속성 오류: {dependencyStatus}");
        }
    }

    /// <summary>
    /// Firebase 인증 상태 변경 이벤트를 처리합니다. 로그인 상태가 변경될 때마다 OnAuthStateChanged 이벤트를 호출합니다.
    /// 추후 진행에 따라 Lobby 씬에서 로그인 상태를 확인하고 UI를 업데이트하도록 변경 필요.
    /// </summary>
    private void HandleAuthStateChanged(object sender, EventArgs e)
    {
        if (auth == null) return;

        if (auth.CurrentUser != user)
        {
            bool signedIn = (auth.CurrentUser != user && auth.CurrentUser != null);
            user = auth.CurrentUser;

            string statusMsg = signedIn ? (user.Email ?? user.DisplayName ?? user.UserId)
                : string.Empty;

            OnAuthStateChanged?.Invoke(signedIn, statusMsg);
        }
    }

    /// <summary>
    /// Firebase 이메일/비밀번호 기반 로그인 메서드입니다. 로그인 성공 시 true, 실패 시 false를 반환합니다.
    /// </summary>
    public async UniTask<bool> SignInWithEmailAsync(string email, string password, CancellationToken ct = default)
    {
        try
        {
            AuthResult authResult = await auth.SignInWithEmailAndPasswordAsync(email, password).AsUniTask().AttachExternalCancellation(ct);
            return authResult != null;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            UtilDebug.LogError($"이메일 로그인 실패: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Firebase 이메일/비밀번호 기반 회원가입 메서드입니다. 회원가입 성공 시 true, 실패 시 false를 반환합니다.
    /// </summary>
    public async UniTask<bool> CreateWithEmailAsync(string email, string password, CancellationToken ct = default)
    {
        try
        {
            AuthResult authResult = await auth.CreateUserWithEmailAndPasswordAsync(email, password).AsUniTask().AttachExternalCancellation(ct);
            return authResult != null;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            UtilDebug.LogError($"회원가입 실패: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Firebase 구글 인증 토큰 기반 로그인 메서드입니다. 로그인 성공 시 true, 실패 시 false를 반환합니다.
    /// </summary>
    public async UniTask<bool> SignInWithGoogleTokenAsync(string idToken, CancellationToken ct = default)
    {
        try
        {
            Credential credential = GoogleAuthProvider.GetCredential(idToken, null);
            FirebaseUser authResult = await auth.SignInWithCredentialAsync(credential).AsUniTask().AttachExternalCancellation(ct);
            return authResult != null;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            UtilDebug.LogError($"구글 인증 실패: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Firebase 계정 삭제 메서드입니다. 로그인된 계정이 없으면 실패를 반환하며, 삭제 성공 시 true, 실패 시 false를 반환합니다.
    /// </summary>
    public async UniTask<(bool success, string errorMessage)> DeleteAccountAsync(CancellationToken ct = default)
    {
        if (user == null)
        {
            return (false, "로그인된 계정이 없습니다.");
        }
        try
        {
            // Firebase 계정 삭제 실행
            await user.DeleteAsync().AsUniTask().AttachExternalCancellation(ct);

            // 삭제 성공 시 유저 참조 초기화
            user = null;
            return (true, string.Empty);
        }
        catch (OperationCanceledException)
        {
            return (false, "작업이 취소되었습니다.");
        }
        catch (Exception ex)
        {
            UtilDebug.LogError($"계정 삭제 실패: {ex.Message}");
            return (false, ex.Message);
        }
    }


    public void SignOut()
    {
        auth?.SignOut();
        user = null;
    }
}