using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class BootstrapController : MonoBehaviour
{
    [SerializeField] private BootstrapView view;

    private const string LOGIN_SCENE = "LoginTest(Server)";
    private const string REMOTE_BUNDLE_LABEL = "미정";

    private void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private void Start()
    {
        RunBootstrapSequenceAsync().Forget();
    }
    private async UniTaskVoid RunBootstrapSequenceAsync()
    {
        var ct = this.GetCancellationTokenOnDestroy();
        //await ExecuteStepWithRetryAsync(
        //    () => StepInitFirebaseAsync(ct),
        //    "",
        //    ct
        //    );

        await ExecuteStepWithRetryAsync(
            () => StepCheckAndDownloadAssetsAsync(ct),
            "",
            ct
            );

        view.UpdateState("로그인 화면으로 이동 중...", 1.0f);
    }

    private async UniTask<bool> StepCheckAndDownloadAssetsAsync(CancellationToken ct = default)
    {
        return false;
    }

    private async UniTask ExecuteStepWithRetryAsync(Func<UniTask<bool>> stepMethod, string retryMessage, CancellationToken ct)
    {
        return;
    }
    private async UniTask StepInitFirebaseAsync(Func<UniTask<bool>> stepMethod, string retryMessage, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                bool isSuccess = await stepMethod.Invoke();
                if (isSuccess) return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Bootstrap Error] {ex.Message}");
            }

            // 실패 시 유저 확인 대기 후 재실행
            await view.ShowErrorPopupAsync(retryMessage, ct);
        }
    }
}
