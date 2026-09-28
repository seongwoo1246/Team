/*담담자 - 송태훈
안드로이드 CredentialManager 네이티브 브릿지를 호출하여 구글 로그인 토큰을 요청
네이티브 콜백으로 전달받은 ID 토큰을 기반으로 Firebase 서버 인증 프로세스를 수행
 */
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GoogleLogin : MonoBehaviour
{
    [SerializeField]
    private string webClientId = "502389656303-70u82ggb4kjpl8spirld6tkjdhaecj3q.apps.googleusercontent.com";

    public event System.Action<string> OnLogStatus;

    /// <summary>
    /// 안드로이드 런타임 환경에서 AndroidJavaClass를 통해 네이티브 Credential Manager 액티비티를 실행합니다
    /// </summary>
    public void RequestGoogleLogin()
    {
        OnLogStatus?.Invoke("구글 로그인 창 호출 중...");

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) 
                {
                    AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    using (AndroidJavaClass helper = new AndroidJavaClass("com.yourcompany.auth.CredentialManagerHelper")) 
                    {
                        // 주의: gameObject.name이 UnitySendMessage를 수신할 오브젝트 이름과 일치해야 함
                        helper.CallStatic("requestGoogleLogin", currentActivity, webClientId, gameObject.name, nameof(OnGoogleTokenReceived));
                    }
                }
            }
            catch (System.Exception ex)
            {
                OnLogStatus?.Invoke($"구글 호출 예외: {ex.Message}");
            }
#else
        OnLogStatus?.Invoke("에디터 환경에서는 지원되지 않습니다.");
#endif
    }

    /// <summary>
    /// Java 브릿지의 UnitySendMessage로 응답받은 구글 ID 토큰을 검증하고 Firebase 로그인 단계(ProcessGoogleSignInAsync)로 넘깁니다
    /// </summary>
    public void OnGoogleTokenReceived(string result) 
    {
        Debug.Log($"[GoogleLogin] 네이티브 결과 수신: {result}");

        if (string.IsNullOrEmpty(result) || result.StartsWith("ERROR:")) 
        {
            OnLogStatus?.Invoke($"구글 로그인 실패: {result}");
            return;
        }

        OnLogStatus?.Invoke("Firebase 서버 인증 처리 중...");
        ProcessGoogleSignInAsync(result).Forget();  
    }

    private async UniTaskVoid ProcessGoogleSignInAsync(string token) 
    {
        var ct = this.GetCancellationTokenOnDestroy();
        bool success = await AuthLoginSystem.Instance.SignInWithGoogleTokenAsync(token, ct); 

        if (!success)
        {
            OnLogStatus?.Invoke("구글 계정 인증 실패");
        }
    }
}