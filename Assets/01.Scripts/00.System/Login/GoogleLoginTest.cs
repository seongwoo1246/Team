using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GoogleLoginTest : MonoBehaviour
{
    [SerializeField] private string webClientId = "502389656303-70u82ggb4kjpl8spirld6tkjdhaecj3q.apps.googleusercontent.com";

    public event Action<string> OnLogStatus;

    public void RequestGoogleLogin()
    {
        OnLogStatus?.Invoke("구글 로그인 창 호출 중...");

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
        OnLogStatus?.Invoke("안드로이드 기기에서만 지원됩니다.");
#endif
    }

    public void OnGoogleTokenReceived(string result)
    {
        if (result.StartsWith("ERROR:"))
        {
            OnLogStatus?.Invoke($"구글 로그인 실패: {result}");
            return;
        }

        OnLogStatus?.Invoke("Firebase 로그인 처리 중...");
        ProcessGoogleSignInAsync(result).Forget();
    }

    private async UniTaskVoid ProcessGoogleSignInAsync(string token)
    {
        bool success = await LoginSystemTest.instance.SignInWithGoogleTokenAsync(token, this.GetCancellationTokenOnDestroy());
        if (!success)
        {
            OnLogStatus?.Invoke("구글 계정 인증 실패");
        }
    }
}