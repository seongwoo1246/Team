/*담담자 - 송태훈
Firebase App 의존성 확인 및 FirebaseAuth 인스턴스를 초기화하고 인증 상태를 관리
이메일/비밀번호 로그인·회원가입, 구글 자격 증명 로그인 및 계정 삭제 비동기 API를 제공
 */
using System;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UtilDebug = DebugLogger<AuthLoginSystem>;

public class AuthLoginSystem : NonMonoSingleton<AuthLoginSystem>
{
    private FirebaseAuth auth;
    private FirebaseUser user;
    private bool isInitialized = false;

    public FirebaseUser CurrentUser => user;
    public string UserId => user != null ? user.UserId : string.Empty;

    public event Action<bool, string> OnAuthStateChanged;

    //
    public bool isLocalTestMode = false;

    /// <summary>
    /// Firebase 의존성 확인 및 FirebaseAuth 초기화를 비동기로 완료 보장
    /// </summary>
    public async UniTask<bool> InitializeFirebaseAsync(System.Threading.CancellationToken ct = default)
    {
        if (isInitialized) return true;

        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync().AsUniTask().AttachExternalCancellation(ct);
            if (dependencyStatus == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                // static으로 인한 user 메모리 저장을 해제하는 임시 처리. 테스트를 위해서. Build 시 삭제
                if (auth.CurrentUser != null)
                {
                    SignOut();
                }
                user = auth.CurrentUser;    // 로그인 된 세션이 있는지 캐싱
                auth.StateChanged += HandleAuthStateChanged;
                isInitialized = true;
                return true;
            }
            else
            {
                UtilDebug.LogError($"Firebase 종속성 오류: {dependencyStatus}");
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            UtilDebug.LogError($"Auth 초기화 예외 발생: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Firebase 인증 상태가 변경될 때 캐싱된 유저 객체를 갱신하고 외부(UI/Controller)로 상태 알림 이벤트 입니다.
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
    public async UniTask<bool> SignInWithEmailAsync(string email, string password, System.Threading.CancellationToken ct = default)
    {
        if(!isInitialized || auth == null)
        {
            UtilDebug.LogError("Auth 인스턴스가 초기화 x");
            return false;
        }

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
    public async UniTask<bool> CreateWithEmailAsync(string email, string password, System.Threading.CancellationToken ct = default)
    {
        if (!isInitialized || auth == null)
        {
            UtilDebug.LogError("Auth 인스턴스가 초기화 x");
            return false;
        }

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
    /// 네이티브 플랫폼에서 수신한 Google IdToken으로 Credential을 생성하여 Firebase에 인증을 요청합니다.
    /// </summary>
    public async UniTask<bool> SignInWithGoogleTokenAsync(string idToken, System.Threading.CancellationToken ct = default)
    {
        if (!isInitialized || auth == null)
        {
            UtilDebug.LogError("Auth 인스턴스가 초기화 x");
            return false;
        }

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
    public async UniTask<(bool success, string errorMessage)> DeleteAccountAsync(System.Threading.CancellationToken ct = default)
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