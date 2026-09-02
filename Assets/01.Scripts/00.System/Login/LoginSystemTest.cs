using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Debug = DebugLogger<LoginSystemTest>;

public class LoginSystemTest : NonMonoSingleton<LoginSystemTest>
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
            Debug.LogError($"Firebase 종속성 오류: {dependencyStatus}");
        }
    }

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

    // 이메일 로그인
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
            Debug.LogError($"이메일 로그인 실패: {ex.Message}");
            return false;
        }
    }

    // 이메일 회원가입
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
            Debug.LogError($"회원가입 실패: {ex.Message}");
            return false;
        }
    }

    // 구글 토큰 기반 로그인
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
            Debug.LogError($"구글 인증 실패: {ex.Message}");
            return false;
        }
    }

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
            Debug.LogError($"계정 삭제 실패: {ex.Message}");
            return (false, ex.Message);
        }
    }


    public void SignOut()
    {
        auth?.SignOut();
        user = null;
    }
}