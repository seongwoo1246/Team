/*담담자 - 송태훈
 클라 내에 내장된 Google ID 토큰을 가지고 Firebase 메서드를 통해 구글 계정으로 서버 내에 계정을 등록 및 로그인 기능을 테스트하는 스크립트.
 */

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GoogleLogin : MonoBehaviour
{
    [SerializeField]
    private string webClientId = "502389656303-70u82ggb4kjpl8spirld6tkjdhaecj3q.apps.googleusercontent.com";

    public event Action<string> OnLogStatus;

    /// <summary>
    /// 버튼 클릭 시 호출
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
            catch (Exception ex)
            {
                OnLogStatus?.Invoke($"구글 호출 예외: {ex.Message}");
            }
#else
        OnLogStatus?.Invoke("에디터 환경에서는 지원되지 않습니다.");
#endif
    }

    /// <summary>
    /// Java 코드에서 UnitySendMessage(gameObject.name, "OnGoogleTokenReceived", result)로 호출됨
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
        bool success = await AuthLoginSystem.instance.SignInWithGoogleTokenAsync(token, ct); 

        if (!success)
        {
            OnLogStatus?.Invoke("구글 계정 인증 실패");
        }
    }
}