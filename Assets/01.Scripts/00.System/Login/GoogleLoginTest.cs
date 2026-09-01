using Firebase;
using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = DebugLogger<FirebaseAuthTest>;
public class GoogleLoginTest : MonoBehaviour
{
    [Header("Google OAuth 설정")]
    [SerializeField] private string webClientId = "502389656303-70u82ggb4kjpl8spirld6tkjdhaecj3q.apps.googleusercontent.com";

    [Header("UI 연결 (TextMeshPro 사용 시)")]
    [SerializeField] private Button loginButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private FirebaseAuth auth;

    private void Start()
    {
        // 버튼 클릭 이벤트 바인딩
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnGoogleSignInClicked);
        }

        UpdateStatus("Firebase 초기화 확인 중...");

        // Firebase 종속성 검사 및 초기화
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                UpdateStatus("로그인 대기 중");
            }
            else
            {
                UpdateStatus($"Firebase 초기화 실패: {task.Result}");
            }
        });
    }

    public void OnGoogleSignInClicked()
    {
        UpdateStatus("구글 로그인 창 호출 중...");

#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using (AndroidJavaClass helper = new AndroidJavaClass("com.yourcompany.auth.CredentialManagerHelper"))
            {
                helper.CallStatic("requestGoogleLogin", currentActivity, webClientId, gameObject.name, nameof(OnGoogleTokenReceived));
            }
        }
#else
        UpdateStatus("Android 기기에서만 지원됩니다 (에디터 테스트 불가)");
#endif
    }

    public void OnGoogleTokenReceived(string result)
    {
        if (result.StartsWith("ERROR:"))
        {
            UpdateStatus($"구글 로그인 실패: {result}");
            return;
        }

        string idToken = result;
        UpdateStatus("구글 토큰 획득! Firebase 인증 중...");
        SignInWithFirebase(idToken);
    }

    private async void SignInWithFirebase(string idToken)
    {
        try
        {
            Credential credential = GoogleAuthProvider.GetCredential(idToken, null);

            // await 호출
            var authResult = await auth.SignInWithCredentialAsync(credential);

            // SDK 버전에 따라 authResult가 AuthResult이거나 FirebaseUser일 수 있음
            FirebaseUser newUser = auth.CurrentUser;

            UpdateStatus($"로그인 성공!\n이름: {newUser.DisplayName}\nUID: {newUser.UserId}");
        }
        catch (System.Exception ex)
        {
            UpdateStatus($"Firebase 오류: {ex.Message}");
        }
    }

    // 메인 스레드/백그라운드 스레드 상태 텍스트 갱신 헬퍼
    private void UpdateStatus(string message)
    {
        Debug.Log(message);
        if (statusText != null)
        {
            // Firebase 비동기 태스크 내부에서 호출될 수 있으므로 유니티 메인 컨텍스트 처리 권장
            UnityMainThreadDispatcher(message);
        }
    }

    private void UnityMainThreadDispatcher(string message)
    {
        // 간단한 텍스트 갱신 (단순 전달용)
        statusText.text = message;
    }
}