/* 담당자 - 송태훈
  
 
 */

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UtilDebug = DebugLogger<GameManager>;

public enum GameState
{
    None,
    Initializing,   // 타이틀 부트스트랩 단계
    Lobby,          // 메인 로비 (파밍 포함)
    Paused          // 게임 일시 정지
}

public class GameManager : Singleton<GameManager>
{
    private readonly System.Collections.Generic.List<ISyncable> _syncables = new();
    private const float AUTO_FLUSH_INTERVAL_SECONDS = 300f;
    private CancellationTokenSource _autoFlushCts;
    private bool _isFlushing = false;

    private GameState _currentState = GameState.None;
    public GameState CurrentState => _currentState;

    public event Action<GameState> OnGameStateChanged;
    protected override void Awake()
    {
        isDDOL = true; // 전역 유지 싱글톤
        base.Awake();
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopPeriodicFlushLoop();
    }

    public void RegisterSyncable(ISyncable syncable)
    {
        if(syncable != null && !_syncables.Contains(syncable))
            _syncables.Add(syncable);
    }
    public void UnregisterSyncable(ISyncable syncable) => _syncables.Remove(syncable);

    /// <summary>
    /// 게임 전역 상태 전환
    /// </summary>
    public void ChangeState(GameState newState)
    {
        if (_currentState == newState) return;

        UtilDebug.Log($"게임 상태 전환: {_currentState} -> {newState}");
        _currentState = newState;
        OnGameStateChanged?.Invoke(_currentState);

        switch (newState)
        {
            case GameState.Lobby:
                Time.timeScale = 1f;
                StartPeriodicFlushLoop(); // 로비 진입 시 5분 자동 동기화 가동
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            default:
                StopPeriodicFlushLoop();
                break;
        }
    }

    #region 앱 생명주기 및 강제 데이터 동기화
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // 백그라운드 전환 시 서버 및 로컬 데이터 강제 플러시
            FlushGameDataAsync().Forget();
        }
    }

    private void OnApplicationQuit()
    {
        // 앱 종료 시 최종 동기화
        FlushGameDataAsync().Forget();
    }


    #region 비정상 종료 / 강제 로그아웃 복귀 처리
    /// <summary>
    /// 세션 만료, 네트워크 단절 등으로 게임을 초기 타이틀 화면으로 안전하게 리셋할 때 호출
    /// </summary>
    public async UniTask ReturnToTitleSceneAsync()
    {
        UtilDebug.LogWarning("타이틀 씬으로 강제 복귀");

        StopPeriodicFlushLoop() ;

        // 1. 서비스 로케이터의 로컬 서비스 일괄 해제
        ServiceLocator.ClearSceneLocalServices();

        // 2. 유저 캐시 정리
        UserManager.Instance.ClearLocalData();

        // 3. 부트스트랩 씬으로 전환
        await SceneLoadManager.Instance.LoadSceneFlowAsync(SceneId.BootstrapScene);
        ChangeState(GameState.Initializing);
    }
    #endregion

    /// <summary>
    /// 5분마다 인게임 데이터를 정기적으로 서버 및 메모리에 플러시하는 백그라운드 루프
    /// </summary>
    private void StartPeriodicFlushLoop()
    {
        StopPeriodicFlushLoop();
        _autoFlushCts = new CancellationTokenSource();
        RunPeriodicFlushLoop(_autoFlushCts.Token).Forget();
    }

    private void StopPeriodicFlushLoop()
    {
        if (_autoFlushCts != null)
        {
            _autoFlushCts.Cancel();
            _autoFlushCts.Dispose();
            _autoFlushCts = null;
        }
    }

    private async UniTaskVoid RunPeriodicFlushLoop(CancellationToken token)
    {
        UtilDebug.Log($"5분 주기 자동 동기화 루프 가동 시작");

        while (!token.IsCancellationRequested)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(AUTO_FLUSH_INTERVAL_SECONDS), cancellationToken: token);

            if (token.IsCancellationRequested) break;

            UtilDebug.Log("정기 자동 동기화(5분) 트리거 실행");
            await FlushGameDataAsync();
        }
    }

    /// <summary>
    /// 재화, 마지막 로그인 시각, 인벤토리 등의 데이터를 한 번에 안전하게 저장
    /// 중요한 이벤트 발생 시 무조건 호출 넣기
    /// </summary>
    public async UniTask FlushGameDataAsync()
    {
        if (_isFlushing)
        {
            UtilDebug.LogWarning("이미 데이터 플러시가 진행 중입니다. 호출을 생략합니다.");
            return;
        }

        _isFlushing = true;
        UtilDebug.Log("전역 데이터 일괄 수집 및 플러시 시작");

        try
        {
            var ct = this.destroyCancellationToken;

            // 1. 등록된 모든 ISyncable의 최신 상태를 UserInfo 메모리로 일괄 취합
            for (int i = 0; i < _syncables.Count; i++)
            {
                _syncables[i]?.SyncToUserMemory();
            }

            // 2. 유저 최종 접속 시간 갱신 및 RTDB 통째 일괄 커밋
            if (UserManager.Instance != null && UserManager.Instance.CurrentUser != null)
            {
                await UserManager.Instance.UpdateLastLoginTimeAsync(ct);
                await UserManager.Instance.SaveAllInfoAsync(ct);
            }

            // 3. PlayerPrefs 강제 디스크 쓰기
            PlayerPrefs.Save();

            UtilDebug.Log("전역 데이터 플러시 완료");
        }
        catch (OperationCanceledException)
        {
            // 작업 취소 정상 대응
        }
        catch (Exception ex)
        {
            UtilDebug.LogError($"데이터 플러시 중 오류 발생: {ex.Message}");
        }
        finally
        {
            _isFlushing = false;
        }
    }
    #endregion
}