/*
 클라 내에 내장된 Google ID 토큰을 가지고 Firebase 메서드를 통해 구글 계정으로 서버 내에 계정을 등록 및 로그인 기능을 테스트하는 스크립트.
 */

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GoogleLogin : MonoBehaviour
{
    [SerializeField]
    private string webClientId = "502389656303-70u82ggb4kjpl8spirld6tkjdhaecj3q.apps.googleusercontent.com"; //

    public event Action<string> OnLogStatus; //

    private AndroidGoogleAuthProvider provider;

    private void Awake()
    {
        // 플랫폼에 맞는 프로바이더 생성
        provider = new AndroidGoogleAuthProvider(webClientId, gameObject.name);
        provider.OnLogStatus += status => OnLogStatus?.Invoke(status);
    }

    /// <summary>
    /// 외부(UI 버튼 등)에서 구글 로그인을 시작할 때 호출
    /// </summary>
    public void RequestGoogleLogin()
    {
        ExecuteGoogleSignInAsync().Forget();
    }

    private async UniTaskVoid ExecuteGoogleSignInAsync()
    {
        OnLogStatus?.Invoke("Google 계정 인증 창 호출 중...");
        string token = await provider.RequestTokenAsync();

        if (string.IsNullOrEmpty(token))
        {
            OnLogStatus?.Invoke("Google 로그인 취소 또는 실패");
            return;
        }

        OnLogStatus?.Invoke("Firebase 서버 인증 처리 중...");
        bool success = await AuthLoginSystem.instance.SignInWithGoogleTokenAsync(token, this.GetCancellationTokenOnDestroy());

        if (!success)
        {
            OnLogStatus?.Invoke("Google 계정 인증 실패");
        }
    }

    /// <summary>
    /// Java -> UnitySendMessage 수신 브릿지 (Java 코드 호환 유지용)
    /// </summary>
    public void OnGoogleTokenReceived(string result) 
    {
        provider?.HandleTokenReceived(result);
    }
}