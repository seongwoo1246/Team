using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
public interface IPlatformAuthProvider
{
    event Action<string> OnLogStatus;
    UniTask<string> RequestTokenAsync();
}

public class AndroidGoogleAuthProvider : IPlatformAuthProvider
{
    private readonly string clientId;
    private readonly string receiverGamejectName;
    private UniTaskCompletionSource<string> tokenUcs;

    public event Action<string> OnLogStatus;

    public AndroidGoogleAuthProvider(string clientId, string receiverGamejectName)
    {
        this.clientId = clientId;
        this.receiverGamejectName = receiverGamejectName;
    }

    public async UniTask<string> RequestTokenAsync()
    {
        OnLogStatus?.Invoke("구글 로그인 창 호출");

#if UNIT_ANDROID && !UNITY_EDITOR
        tokenUcs = new UniTaskCompletionSource<string>();


        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using (AndroidJavaClass googleAuthClass = new AndroidJavaClass("com.example.googleauth.GoogleAuth"))
                {
                    googleAuthClass.CallStatic("requestGoogleLogin", currentActivity, clientId, receiverGamejectName);
                }
            }
            return await tokenUcs.Task;
        }

        catch (Exception ex)
        {
            OnLogStatus?.Invoke($"Android Credential Manager 호출 실패: {ex.Message}");
            return null;
        }
#else
        OnLogStatus?.Invoke("에디터 환경에서는 지원 X");
        await UniTask.Yield();
        return null;
#endif
    }

    public void HandleTokenReceived(string result)
    {
        if (string.IsNullOrEmpty(result) || result.StartsWith("ERROR:"))
        {
            OnLogStatus?.Invoke($"구글 로그인 실패: {result}");
            tokenUcs?.TrySetResult(null);
            return;
        }
        OnLogStatus?.Invoke($"구글 로그인 성공");
        tokenUcs?.TrySetResult(result);
    }
}