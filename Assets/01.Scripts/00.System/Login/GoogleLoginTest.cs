/*
 클라 내에 내장된 Google ID 토큰을 가지고 Firebase 메서드를 통해 구글 계정으로 서버 내에 계정을 등록 및 로그인 기능을 테스트하는 스크립트.
 */

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GoogleLoginTest : MonoBehaviour
{
    // 구글 클라이언트 ID (Firebase 프로젝트 설정에서 확인 가능)
    [SerializeField] private string webClientId = "502389656303-70u82ggb4kjpl8spirld6tkjdhaecj3q.apps.googleusercontent.com";

    public event Action<string> OnLogStatus;    // 로그인 상태 메시지 이벤트

    /// <summary>
    /// Java 코드를 통해 외부 API CredentialManager를 연결하는 메서드. 구글 로그인 창을 호출하고, 결과를 OnGoogleTokenReceived 메서드로 전달.
    /// </summary>
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

    /// <summary>
    /// Firebase 로그인 처리를 위해 구글 로그인 결과 토큰을 받아 처리하는 메서드. Java 코드에서 호출됨.
    /// </summary>
    /// <param name="result"></param>
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

    /// <summary>
    /// Firebase에 구글 토큰을 사용하여 로그인 시도. 성공 여부에 따라 상태 메시지를 업데이트.
    /// </summary>
    private async UniTaskVoid ProcessGoogleSignInAsync(string token)
    {
        bool success = await LoginSystemTest.instance.SignInWithGoogleTokenAsync(token, this.GetCancellationTokenOnDestroy());
        if (!success)
        {
            OnLogStatus?.Invoke("구글 계정 인증 실패");
        }
    }
}