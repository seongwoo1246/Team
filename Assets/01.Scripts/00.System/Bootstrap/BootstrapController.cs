/*
담담자 - 송태훈
 게임 실행 시 가장 먼저 해야하는 일들을 순서대로 진행할 수 있도록 하는 컨트롤러
1. Firebase 서버 확인 및 Manager Init
2. 계정 로그인 및 없을 시 생성
3. Addresaable 카탈로그 검사 및 다운로드
4. 다운로드 확인 후 LoginScene으로 전환

추가로 모든 클래스에 대한 정의와 데이터 및 초기화 관련 선언이 완성된 후 DataManager와 각 Manager에 대한 초기화 순서를 여기서 지정해줄 예정
 */

using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UtilDebug = DebugLogger<BootstrapController>;

public class BootstrapController : MonoBehaviour
{
    [Header("View & LoginController")]
    [SerializeField] private BootstrapView view;
    [SerializeField] private LoginController loginController;

    private const string LOBBY_SCENE_KEY = "LobbySceneTest(Server)";
    private const string REMOTE_LABEL = "RemoteScenes";

    private void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private void Start()
    {
        RunBootstrapSequenceAsync().Forget();
    }

    /// <summary>
    /// Bootstrap 순차적 진행 비동기 메서드
    /// 1. 인프로 초기화(매니저) → 2. 유저 로그인 → 3. CDN 리소스 패치 → 4. 로비 이동
    /// </summary>
    /// <returns></returns>
    private async UniTaskVoid RunBootstrapSequenceAsync()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        // STEP 1. Firebase(유저 데이터 저장 서버) 의존성 검사 & 시스템 매니저 초기화
        view.SetLoadingVisible(true);
        view.UpdateState("서버 연결 확인 중...", 0.0f);

        await ExecuteStepWithRetryAsync(
            () => StepInitFirebaseAsync(ct),
            "서버 연결에 실패했습니다. 네트워크 환경을 확인", ct);

        await StepInitManagerAsync(ct);
        
        view.UpdateState("초기화 완료", 1.0f);
        await UniTask.Delay(300, cancellationToken: ct);

        // STEP 2. 로그인 단계 실행 ( 완료될 때까지 대기 )
        view.SetLoadingVisible(false);
        await loginController.LoginFlowAsync(ct);

        // STEP 3. CDN 에셋 번들 일괄 검사 및 다운로드
        view.SetLoadingVisible(true);
        view.UpdateState("패치 데이터 확인 중...", 0.0f);
        await ExecuteStepWithRetryAsync(
            () => StepCheckAndDownloadAssetsAsync(ct),
            "데이터 다운로드에 실패했습니다. 다시 시도",
            ct
            );

        view.UpdateState("다운로드 완료", 0.0f);

        // STEP 4. 모든 비동기 처리 완료. 로비 씬 전환
        view.UpdateState("로비로 이동 중...", 1.0f);
        await UniTask.Delay(150, cancellationToken: ct);
        SceneLoaderManager.instance.LoadSceneFlowAsync(LOBBY_SCENE_KEY, async () =>
        {
            await UniTask.Yield(); 
        }).Forget();
    }

    #region STEP 1. 인프로 초기화
    /// <summary>
    /// Firebase의 의존성 검사 및 Auth 초기화를 보장하는 비동기 메서드
    /// </summary>
    private async UniTask<bool> StepInitFirebaseAsync(CancellationToken ct = default)
    {
        view.UpdateState("서버 확인 중...", 0.1f);
        bool authReady = await AuthLoginSystem.instance.InitializeFirebaseAsync(ct);
        return authReady;
    }

    private async UniTask StepInitManagerAsync(CancellationToken ct)
    {
        view.UpdateState("시스템 초기화 중...", 0.2f);

        // 매니저 초기화 순서 추후에 지정해야함

        // 싱글톤 매니저 초기화 보장
        AddressableManager.instance.Init();
        SceneLoaderManager.instance.Init();

        // 논싱글톤 매니저 초기화 보장
        UserManager.instance.Init();

        await UniTask.Yield(PlayerLoopTiming.Update, ct);
    }
    #endregion

    #region STEP 3. 리소스 다운로드
    /// <summary>
    /// CDN 에서 RemoteAssets 모든 원격 번들 검사 및 다운로드를 진행하는 비동기 메서드
    /// </summary>
    private async UniTask<bool> StepCheckAndDownloadAssetsAsync(CancellationToken ct)
    {
        view.UpdateState("패치 데이터 확인 중...", 0.35f);
        // 에셋 용량 합산 검사
        long downlaodSize = await AddressableManager.instance.CheckDownladSizeAsync(REMOTE_LABEL, ct);

        if (downlaodSize > 0)
        {
            // 다운로드 팝업 노출 및 유저 승인 대기
            bool isApproved = await view.ShowDownloadPopupAsync(downlaodSize, ct);
            if (!isApproved)
            {
                Application.Quit();
                return false;
            }

            // 번들 다운로드 진행률 매핑 (0.3 ~ 0.95)
            view.UpdateState("리소스 패치 다운로드 중...", 0.4f);
            return await AddressableManager.instance.DownloadDependenciesAsync(
                REMOTE_LABEL, progress =>
                {
                    float mapped = Mathf.Lerp(0.4f, 0.95f, progress);
                    view.UpdateState("리소스 패치 다운로드 중...", mapped);
                },
                ct
             );
        }

        return true;
    }
    #endregion

    #region 리트라이
    /// <summary>
    /// 단계 실행 중 예외 또는 false 반환 시 에러 팝업을 띄우고 재시도 버튼을 누를 때까지 루프를 반복하는 비동기 메서드
    /// </summary>
    private async UniTask ExecuteStepWithRetryAsync(Func<UniTask<bool>> stepMethod, string retryMessage, CancellationToken ct)
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
                UtilDebug.LogError($"{ex.Message}");
            }

            // 실패 시 유저 확인 대기 후 재실행
            await view.ShowErrorPopupAsync(retryMessage, ct);
        }
    }
    #endregion

}
