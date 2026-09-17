using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
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
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
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

    /// <summary>
    /// 재화, 마지막 로그인 시각, 인벤토리 등의 데이터를 한 번에 안전하게 저장
    /// </summary>
    public async UniTaskVoid FlushGameDataAsync()
    {
        UtilDebug.Log("앱 상태 변화로 인한 전역 데이터 저장 시작");

        try
        {
            var ct = this.destroyCancellationToken;

            // 1. 유저 최종 접속 시간 갱신
            if (UserManager.Instance != null && UserManager.Instance.CurrentUser != null)
            {
                await UserManager.Instance.UpdateLastLoginTimeAsync(ct);
                await UserManager.Instance.SaveAllInfoAsync(ct);
            }

            // 2. PlayerPrefs 강제 디스크 쓰기
            PlayerPrefs.Save();

            UtilDebug.Log("전역 데이터 플러시 완료");
        }
        catch (Exception ex)
        {
            UtilDebug.LogError($"데이터 플러시 중 오류 발생: {ex.Message}");
        }
    }
    #endregion

    #region 비정상 종료 / 강제 로그아웃 복귀 처리
    /// <summary>
    /// 세션 만료, 네트워크 단절 등으로 게임을 초기 타이틀 화면으로 안전하게 리셋할 때 호출
    /// </summary>
    public async UniTask ReturnToTitleSceneAsync()
    {
        UtilDebug.LogWarning("타이틀 씬으로 강제 복귀");

        // 1. 서비스 로케이터의 로컬 서비스 일괄 해제
        ServiceLocator.ClearSceneLocalServices();

        // 2. 유저 캐시 정리
        UserManager.Instance.ClearLocalData();

        // 3. 부트스트랩 씬으로 전환
        await SceneLoadManager.Instance.LoadSceneFlowAsync(SceneId.BootstrapScene);
        ChangeState(GameState.Initializing);
    }
    #endregion
}