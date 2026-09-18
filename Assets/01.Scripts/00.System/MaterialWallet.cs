// 작성자: 김주연
/*
특별 강화재료 지갑. 장비 강화(+10까지)에 쓰는 재화. 골드랑 다르게 딱 떨어지는 개수(int)로 관리함

얻는 방법 3가지:
  1. 황금 고블린(파밍 중 아주 낮은 확률로 스폰되는 특수 몬스터) 처치 - GoldenGoblin.cs가 직접 Add(1) 호출
  2. 온+오프라인 합산 플레이시간이 secondsPerMaterial(기본 10시간)만큼 쌓일 때마다 1개 - 여기서 직접 관리
     딱 1개씩만 지급하는 방식이라 오프라인으로 아무리 오래 떨어져있었어도 한번에 1개까지만 나옴
     (AccumulateSeconds가 if문 한 번만 검사하지 while로 여러번 안도는 구조라 자동으로 그렇게 됨)
  3. 챌린지 스테이지 클리어(보스 처치) - StageManager.StageCleared를 직접 구독해서 확정지급

싱글톤은 팀 공용 Singleton<T> 상속
*/
/* 공동 작성자 - 송태훈
   - 수량: UserManager.CurrentUser.Inventory.Data.consumables["Material"] 연동
   - 오프라인 보상: UserProfile.lastLoginTimestamp (서버 기준 시각) 연동 및 변경
   - 플러시 동기화: ISyncable 구현을 통한 GameManager 일괄 수집 지원
 */

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UtilDebug = DebugLogger<MaterialWallet>;
/// <summary>
/// 특별 강화재료 지갑. 씬에 하나 두고 MaterialWallet.instance로 접근
/// </summary>
public sealed class MaterialWallet : Singleton<MaterialWallet>, ILoadable, ISyncable
{
    [Header("플레이시간 보상")]
    [Tooltip("이만큼의 시간(초)이 쌓일 때마다 재료 1개 지급. 기본 36000초 = 10시간")]
    [SerializeField] private float secondsPerMaterial = 36000f;

    [Tooltip("온라인 누적 시간을 몇 초마다 체크할지")]
    [SerializeField] private float tickInterval = 1f;

    private const string MATERIAL_KEY = "Material";
    // 저장 키
    private const string ACCUMULATED_SECONDS_KEY = "MaterialWallet_AccumulatedSeconds";
    //private const string MATERIAL_COUNT_KEY = "MaterialWallet_Count"; - 메서드 미사용으로 변경함으로서 변수 미사용
    //private const string LAST_SEEN_UTC_KEY = "MaterialWallet_LastSeenUtc"; - 메서드 미사용으로 변경함으로서 변수 미사용

    // 보유 재료 개수
    private int _materialCount;

    // 지금까지 쌓인 시간(초). secondsPerMaterial을 채우면 1개 지급하고 0으로 리셋됨
    private float _accumulatedSeconds;

    // Start에서 구독할 때 캐싱해두고 OnDestroy에서 구독 해제할 때 이 캐시로만 접근한다.
    // StageManager.instance를 OnDestroy에서 다시 호출하면, 씬이 꺼지는 순간 이미 원본이 파괴된 뒤라
    // Singleton<T>의 "없으면 새로 만드는" 로직이 발동해서 씬 종료 직전에 새 오브젝트가 하나 생겨버림
    //private StageManager _stageManager;

    // 보유 재료 개수
    public int MaterialCount => _materialCount;

    // 재료 개수가 바뀔 때마다 발생 (인자 = 변경 후 개수). UI 갱신용
    public event Action<int> MaterialCountChanged;

    #region 추가 작업
    public int LoadOrder => 20;
    private bool _isInitialized = false;
    private System.Threading.CancellationTokenSource _loopCts;
    #endregion

    protected override void Awake()
    {
        isDDOL = true;
        base.Awake();
        SceneLoadManager.Instance.RegisterLoadable(this);
        GameManager.Instance.RegisterSyncable(this);
        //_materialCount = PlayerPrefs.GetInt(MATERIAL_COUNT_KEY, 0);
        //_accumulatedSeconds = Mathf.Max(0f, PlayerPrefs.GetFloat(ACCUMULATED_SECONDS_KEY, 0f));
    }

    #region ILoadable + ISyncable 구현 - 송태훈

    public UniTask OnSceneLoadCreate(SceneId scene) => UniTask.CompletedTask;

    public void Init(SceneId scene)
    {
        if (scene != SceneId.LobbySceneTest) return;
        if (_isInitialized) return;

        UtilDebug.Log($"[{scene}] MaterialWallet 초기화 및 서버 인벤토리 연동");

        // 서버 인벤토리에서 재료 수량 동기화 
        UserInfo user = UserManager.Instance.CurrentUser;
        if (user != null && user.Inventory != null && user.Inventory.Data != null)
        {
            if (user.Inventory.Data.consumables.TryGetValue(MATERIAL_KEY, out int count))
            {
                _materialCount = count;
                UtilDebug.Log($"서버 Material 동기화 완료: {_materialCount}개");
            }
            else
            {
                _materialCount = 0;
                user.Inventory.Data.consumables[MATERIAL_KEY] = 0;
            }
            UtilDebug.Log($"서버 재료 동기화 완료: {_materialCount}개");
        }
        else
        {
            _materialCount = 0;
            UtilDebug.LogWarning("서버 인벤토리 데이터가 존재하지 않아 0개로 초기화.");
        }

        // 미지금 잔여 누적 시간만 복원
        _accumulatedSeconds = Mathf.Max(0f, PlayerPrefs.GetFloat(ACCUMULATED_SECONDS_KEY, 0f));
        MaterialCountChanged?.Invoke(_materialCount);

        // 서버 lastLoginTimeStamp 기반 오프라인 보상 정산
        ApplyOfflineTimeFromServer();

        // 스테이지 클리어 보상 이벤트 등록 - StagetManager 수정할 경우 무조건 변경할 부분
        //_stageManager = StageManager.Instance;

        if (ServiceLocator.TryGet<StageManager>(out StageManager _stageManager))
        { 
            _stageManager.StageCleared += OnStageCleared;
        }

        // 누적 시간 루프 가동
        _loopCts?.Cancel();
        _loopCts = new System.Threading.CancellationTokenSource();
        RunPassiveTimeLoop(_loopCts.Token).Forget();

        _isInitialized = true;
    }

    public void OnSceneDestory(SceneId scene)
    {
        CleanUp();
    }

    public void SyncToUserMemory()
    {
        var inventory = UserManager.Instance.CurrentUser?.Inventory;
        if (inventory != null && inventory.Data != null)
        {
            inventory.Data.consumables["Material"] = _materialCount;
        }
    }
    #endregion

    private void Start()
    {
        //ApplyOfflineTime();

        //_stageManager = StageManager.Instance;
        //if (_stageManager != null)
        //{
        //    _stageManager.StageCleared += OnStageCleared;
        //}

        //RunPassiveTimeLoop(this.GetCancellationTokenOnDestroy()).Forget();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterSyncable(this);
        }
        CleanUp();
    }

    /// <summary>
    /// 루프 취소, 이벤트 구독 해제 -송태훈
    /// </summary>
    private void CleanUp()
    {
        if (!_isInitialized) return;
        if (_loopCts != null)
        {
            _loopCts?.Cancel();
            _loopCts?.Dispose();
            _loopCts = null;
        }

        if (ServiceLocator.TryGet<StageManager>(out StageManager _stageManager))
        {
            _stageManager.StageCleared -= OnStageCleared;
        }

        SaveAccumulatedSecond();
        _isInitialized = false;
    }

    // 앱이 완전히 꺼질 때 (빌드 기준) - GameManager에서 관리
    //private void OnApplicationQuit()
    //{
    //    SaveAccumulatedSecond();
    //}

    // 모바일에서 백그라운드로 내려갈 때도 종료에 준해서 저장 - GameManager에서 관리
    //private void OnApplicationPause(bool isPaused)
    //{
    //    if (isPaused)
    //    {
    //        Save();
    //    }
    //}

    /// <summary>
    /// 재료를 더한다 (황금 고블린 처치, 보스 클리어 등에서 호출)
    /// </summary>
    /// <param name="amount">추가할 개수 (0 이하는 무시)</param>
    public void Add(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        _materialCount += amount;
        MaterialCountChanged?.Invoke(_materialCount);

        // 서버 메모리 및 RTDB 동기화
        SyncMaterialToServer();
    }

    /// <summary>
    /// 재료가 충분하면 차감하고 true, 부족하면 아무 것도 안 하고 false (장비 강화 비용 지불용)
    /// </summary>
    /// <param name="amount">차감할 개수</param>
    public bool TrySpend(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (_materialCount < amount)
        {
            return false;
        }

        _materialCount -= amount;
        MaterialCountChanged?.Invoke(_materialCount);

        // 서버 메모리 및 RTDB 동기화
        SyncMaterialToServer();
        return true;
    }

    /// <summary>
    /// 수량 변경 사항을 서버 유저 인벤토리에 즉시 반영 - 송태훈
    /// </summary>
    private void SyncMaterialToServer()
    {
        UserInfo user = UserManager.Instance.CurrentUser;
        if (user != null && user.Inventory != null && user.Inventory.Data != null)
        {
            user.Inventory.Data.consumables[MATERIAL_KEY] = _materialCount;
            UserManager.Instance.UpdateConsumableCountAsync(MATERIAL_KEY, _materialCount, this.destroyCancellationToken).Forget();
        }
    }


    /// <summary>
    /// 챌린지 스테이지를 클리어(보스 처치)했을 때 확정으로 재료1개 지급
    /// </summary>
    /// <param name="stageNumber">방금 클리어한 스테이지 번호 (여기선 안 씀)</param>
    private void OnStageCleared(int stageNumber)
    {
        Add(1);
    }

    /// <summary>
    /// tickInterval마다 온라인 누적 시간을 늘리는 루프
    /// </summary>
    /// <param name="token">파괴 시 루프를 멈추는 취소 토큰</param>
    private async UniTaskVoid RunPassiveTimeLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(tickInterval), cancellationToken: token);
            AccumulateSeconds(tickInterval);
            //SaveLastSeenNow();
        }
    }

    /// <summary>
    /// 누적 시간을 늘리고, 기준(secondsPerMaterial)을 채웠으면 재료 1개 지급 후 초과분은 버리고 0으로 리셋
    /// if문(while이 아님)이라 아무리 큰 값을 한 번에 넣어도 이 호출로는 최대 1개까지만 지급됨
    /// - 오프라인으로 오래 떨어져있다 왔을 때 몰아서 여러개가 아니라 그래도 1개까지만이 되는 이유가 이거임
    /// </summary>
    /// <param name="seconds">추가로 누적할 시간(초)</param>
    private void AccumulateSeconds(float seconds)
    {
        _accumulatedSeconds += seconds;

        if (_accumulatedSeconds >= secondsPerMaterial)
        {
            _accumulatedSeconds = 0f;
            Add(1);
        }
    }

    /// <summary> 
    /// 마지막으로 저장해둔 시각과 지금 시각을 비교해서, 꺼져있던 시간만큼 누적시간에 더한다
    /// (AccumulateSeconds 자체가 한 번에 1개까지만 지급하므로 여기서 따로 상한을둘 필요는 없음)
    /// 서버의 lastLoginTimeStamp 기준으로 정산하기 때문에 미사용을 변경
    /// </summary>
    private void ApplyOfflineTime()
    {
        //string savedText = PlayerPrefs.GetString(LAST_SEEN_UTC_KEY, string.Empty);
        //int materialCountBeforeOffline = _materialCount;

        //if (!string.IsNullOrEmpty(savedText)
        //    && DateTime.TryParse(savedText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime lastSeen))
        //{
        //    double elapsedSeconds = (DateTime.UtcNow - lastSeen).TotalSeconds;
        //    if (elapsedSeconds > 0d)
        //    {
        //        AccumulateSeconds((float)elapsedSeconds);
        //    }
        //}

        //SaveLastSeenNow();

        //// AccumulateSeconds가 한 번 호출로 최대 1개까지만 지급하므로, 여기서 늘어난 만큼(0 또는 1)이 오프라인 지급분
        //int grantedByOffline = _materialCount - materialCountBeforeOffline;

        //// RewardManager(복귀 보상 팝업)는 아직 Inspector 연결이 안 끝난 상태일 수 있어서
        //// instance/필드 둘 다 null 체크하고 지나감 (없어도 재료 지급 자체는 이미 끝난 뒤라 안전함)
        //if (grantedByOffline > 0 && RewardManager.Instance != null && RewardManager.Instance.GetUpgardMaterial != null)
        //{
        //    RewardManager.Instance.GetUpgardMaterial.text = grantedByOffline.ToString();
        //}
    }

    /// <summary>
    /// 서버 lastLoginTimeStamp와 현재 UTC 시간을 비교하여 오프라인 누적 시간을 정산
    /// </summary>
    private void ApplyOfflineTimeFromServer()
    {
        var profile = UserManager.Instance.CurrentUser?.Profile;
        if (profile != null || profile.lastLoginTimestamp <= 0) return;

        long nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long lastLogin = profile.lastLoginTimestamp;

        // 밀리초 단위로 들어온 경우 초 단위로 자동 보정
        if (lastLogin > 1000000000000L)
            lastLogin /= 1000L;

        long elapsedSeconds = nowSeconds - lastLogin;
        int countBefore = _materialCount;

        if(elapsedSeconds > 0)
        {
            AccumulateSeconds((float)elapsedSeconds);
            UtilDebug.Log($"서버 기준 오프라인 시간 적용: {elapsedSeconds}초 경과");
        }
        // 보상 팝업 UI 표시
        int granted = _materialCount - countBefore;
        if (granted > 0 && RewardManager.Instance != null && RewardManager.Instance.GetUpgardMaterial != null)
        {
            RewardManager.Instance.GetUpgardMaterial.text = granted.ToString();
        }

        // 오프라인 정산 완료 즉시 서버 최종 접속 시간을 현재로 갱신
        UserManager.Instance.UpdateLastLoginTimeAsync(this.destroyCancellationToken).Forget();
    }

    /// <summary>
    /// 미완성 타이머 저장
    /// </summary>
    private void SaveAccumulatedSecond()
    {
        PlayerPrefs.SetFloat(ACCUMULATED_SECONDS_KEY, _accumulatedSeconds);
        PlayerPrefs.Save();
    }

    //private void SaveLastSeenNow() // PlayerPrefs 미사용으로 변경
    //{
    //    PlayerPrefs.SetString(LAST_SEEN_UTC_KEY, DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
    //}

    //private void Save() // PlayerPrefs 미사용으로 변경
    //{
    //    PlayerPrefs.SetInt(MATERIAL_COUNT_KEY, _materialCount);
    //    PlayerPrefs.SetFloat(ACCUMULATED_SECONDS_KEY, _accumulatedSeconds);
    //    SaveLastSeenNow();
    //    PlayerPrefs.Save();
    //}
}
