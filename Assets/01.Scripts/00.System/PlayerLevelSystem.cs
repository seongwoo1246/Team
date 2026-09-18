// 작성자: 김주연
/*
플레이어(계정) 레벨 + 경험치 시스템. 캐릭터 개별 스탯이랑은 완전히 별개!!

경험치는 골드처럼 시간에 따라 자동으로 쌓인다 (분당 고정량, 스테이지 진행이랑 무관 - 의도적으로 안 태움)
  → 스테이지를 잘 깨는 사람이 레벨업도 같이 빨라지면, 강화 상한이 사실상 의미 없어지기 때문에
    경험치만큼은 "순수 플레이 시간"에만 비례하게 고정해둠

레벨업에 필요한 경험치는 레벨 구간(tiers)마다 다르게 고정돼있음 (지수 공식이 아니라 계단식)
  예: 1~100레벨은 레벨업당 20, 101~300레벨은 100, ... 이런 식으로 구간마다 정해둔 값을 그대 씀
  (StageRosterData가 몇스테이지부터 어떤 몬스터가 나오는지 구간으로 관리하는 것과 같은 패턴)

이 레벨은 UpgradeSystem에서 각 강화 트랙은 플레이어 레벨을 못넘는다는 상한으로 쓰인다
  (UpgradeSystem.TryUpgrade가 이 시스템의 Level을 직접 참조함)

싱글톤은 팀 공용 Singleton<T> 상속
*/
/* 공동 작성자 - 송태훈
 
 
 */

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UtilDebug = DebugLogger<PlayerLevelSystem>;

/// <summary>
/// 플레이어 레벨/경험치 관리 씬에 하나 두고 PlayerLevelSystem.instance로 접근
/// </summary>
public sealed class PlayerLevelSystem : Singleton<PlayerLevelSystem>, ILoadable, ISyncable
{
    // 구간 목록이 비어있을 때(설정 안 됐을 때) 쓰는 대체값
    private const float DEFAULT_EXP_PER_LEVEL = 20f;

    // DataManger에서 공급받는 정적 데이터
    [field: SerializeField] public PlayerLevelConfig _config { get; private set; }

    // 현재 레벨. 1부터 시작
    private int _level = 1;

    // 현재 레벨 안에서 쌓인 경험치 (레벨업하면 0으로 돌아가고 남은 만큼만 이월됨)
    private float _currentExp;

    // 레벨이 오를 때마다 발생. 인자 = 새 레벨. UI 갱신용
    public event Action<int> LevelUp;

    // 현재 레벨
    public int Level => _level;

    // 현재 레벨 안에서 쌓인 경험치
    public float CurrentExp => _currentExp;

    // 다음 레벨업까지 필요한 경험치 (지금 레벨 기준)
    public float ExpRequiredForNextLevel => GetRequiredExp(_level);

    // 현재 레벨 진행률 (0~1). 경험치 바 UI가 이 값을 읽으면 됨
    public float ExpProgressRatio => ExpRequiredForNextLevel > 0f ? Mathf.Clamp01(_currentExp / ExpRequiredForNextLevel) : 0f;
    
    #region 추가 변수 - 송태훈
    public int LoadOrder => 12;
    private bool _isInitialized = false;
    private System.Threading.CancellationTokenSource _loopCts;
    #endregion

    protected override void Awake()
    {
        base.Awake();
        SceneLoadManager.Instance.RegisterLoadable(this);
        GameManager.Instance.RegisterSyncable(this);
    }

    #region ILoadable + ISyncable 구현부 - 송태훈
    public UniTask OnSceneLoadCreate(SceneId scene)
    {
        _config = DataManager.Instance.GetSingle<PlayerLevelConfig>();
        if(_config == null)
        {
            UtilDebug.LogError("PlayerLevelConfig를 DataManager에서 찾을 수 없습니다.");
        }
        // 에러에 대한 처리를 수정해야함
        return UniTask.CompletedTask;
    }
    public void Init(SceneId scene)
    {
        if (scene != SceneId.LobbySceneTest) return;
        if(_isInitialized) return;

        UtilDebug.Log($"초기화 및 서버 유저 데이터 동기화 시작");

        // 서버 데이터 바인딩
        var profile = UserManager.Instance.CurrentUser?.Profile;
        if(profile != null)
        {
            _level = Mathf.Max(1, profile.accountLevel);
            _currentExp = Mathf.Max(1, profile.currentExp);
            UtilDebug.Log($"서버 레벨 동기화 완료 : Lv.{_level}, Exp : {_currentExp}");
        }
        else
        {
            _level = 1;
            _currentExp = 0f;
            UtilDebug.LogWarning("서버 유저 프로필 부재 - 기본값(Lv.1, Exp : 0)로 기본 초기화 세팅");
        }

        LevelUp?.Invoke(_level);

        // 
        ApplyOfflineExpFromServer();

        _loopCts?.Cancel();
        _loopCts = new CancellationTokenSource();
        RunPassiveExpLoop(_loopCts.Token).Forget();
        
        _isInitialized = true;
    }

    public void OnSceneDestory(SceneId scene)
    {
        CleanUp();
    }

    public void SyncToUserMemory()
    {
        var profile = UserManager.Instance.CurrentUser?.Profile;
        if (profile != null)
        {
            profile.accountLevel = _level;
            profile.currentExp = _currentExp;
        }
    }
    #endregion
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if(GameManager.Instance != null )
        {
            GameManager.Instance.UnregisterSyncable(this);
        }
        CleanUp();
    }

    /// <summary>
    /// 루프 취소, 서버에 보낼 데이터로 최신화
    /// </summary>
    private void CleanUp()
    {
        if(!_isInitialized) return;

        if(_loopCts != null)
        {
            _loopCts?.Cancel();
            _loopCts?.Dispose();
            _loopCts = null;
        }

        SyncToUserMemory();
        _isInitialized = false;
    }


    /// <summary>
    /// tickInterval마다 분당 경험치만큼 나눠서 지급하는 루프. saveInterval마다 한 번씩 저장도 같이함
    /// </summary>
    /// <param name="token">파괴 시 루프를 멈추는 취소 토큰</param>
    private async UniTaskVoid RunPassiveExpLoop(CancellationToken token)
    {
        float interval = _config != null ? _config.TickInterval : 1f;
        float basePerMin = _config != null ? _config.BaseExpPerMinute : 20f;

        while (!token.IsCancellationRequested)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: token);

            float perTick = basePerMin * (interval / 60f);
            AddExp(perTick);
        }
    }

    /// <summary>
    /// 경험치를 더한다. 레벨업 조건을 넘으면 필요한 만큼 여러 번이라도 레벨업시키고
    /// 레벨업이 실제로 일어났으면 그 즉시 저장해서 레벨만큼은 최대한 안전하게 보존
    /// </summary>
    /// <param name="amount">추가할 경험치 (0 이하는 무시)</param>
    private void AddExp(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        _currentExp += amount;
        bool leveledUp = false;

        // while로 처리 - 한 번에 여러 레벨을 넘길 만큼 경험치가 몰려도 전부 반영되게
        while (_currentExp >= GetRequiredExp(_level))
        {
            _currentExp -= GetRequiredExp(_level);
            _level++;
            leveledUp = true;
        }

        // 메모리 상시 반영 ( 5분 자동 플러시 대비 )
        SyncToUserMemory();

        if (leveledUp)
        {
            UtilDebug.Log($"플레이어 레벨업 달성: Lv {_level}");
            LevelUp?.Invoke(_level);

            var ct = this.destroyCancellationToken;
            var profile = UserManager.Instance.CurrentUser?.Profile;
            if(profile != null)
            {
                profile.UpdateSingleFieldAsync(StringConsts.UserConstants.AccountLevel, _level, ct).Forget();
                profile.UpdateSingleFieldAsync(StringConsts.UserConstants.CurrentStage, _currentExp, ct).Forget();
            }
        }
    }
    
    /// <summary>
    /// 서버 lastLoginTimestamp 기반 오프라인 경험치 정산
    /// </summary>
    private void ApplyOfflineExpFromServer()
    {
        var profile = UserManager.Instance.CurrentUser?.Profile;
        if (profile == null || profile.lastLoginTimestamp <= 0) return;
        
        long nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long lastLogin = profile.lastLoginTimestamp;

        // 밀리초 단위 보정
        if (lastLogin > 100000000000L)
        {
            lastLogin /= 1000L;
        }

        long elapsedSeconds = nowSeconds - lastLogin;
        if (elapsedSeconds <= 0) return;

        double maxHours = _config != null ? _config.MaxOfflineHours : 12d;
        float basePerMin = _config != null ? _config.BaseExpPerMinute : 20f;

        double cappedSeconds = Math.Max(0d, Math.Min(elapsedSeconds, maxHours * 3600d));
        double offlineMinutes = cappedSeconds / 60d;

        if(offlineMinutes > 0d)
        {
            float rewardExp = (float)(basePerMin * offlineMinutes);
            AddExp(rewardExp);
            UtilDebug.Log($"서버 기준 오프라인 경험치 지급 완료 : {offlineMinutes:F1}분치 ({rewardExp:F0} Exp)");
            
            // 복귀 팝업 UI 연결
            if (RewardManager.Instance != null && RewardManager.Instance.GetPlayerExp != null)
            {
                RewardManager.Instance.GetPlayerExp.text = rewardExp.ToString("F0");
            }
        }
    }

    /// <summary>
    /// 지정한 레벨에서 레벨업 1회에 필요한 경험치를 구간 목록에서 찾아 돌려줌
    /// 구간 목록이 비어있으면 DEFAULT_EXP_PER_LEVEL을 대신씀
    /// </summary>
    /// <param name="level">기준 레벨</param>
    private float GetRequiredExp(int targetLevel)
    {
        if (_config == null || _config.Tiers == null || _config.Tiers.Count == 0)
        {
            return DEFAULT_EXP_PER_LEVEL;
        }

        float result = DEFAULT_EXP_PER_LEVEL;
        for (int i = 0; i < _config.Tiers.Count; i++)
        {
            if (_config.Tiers[i].StartLevel <= targetLevel)
            {
                result = _config.Tiers[i].ExpPerLevelUp;
            }
            else
            {
                break;
            }
        }

        return result;
    }
}
