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

using System;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 특별 강화재료 지갑. 씬에 하나 두고 MaterialWallet.instance로 접근
/// </summary>
public sealed class MaterialWallet : Singleton<MaterialWallet>
{
    [Header("플레이시간 보상")]
    [Tooltip("이만큼의 시간(초)이 쌓일 때마다 재료 1개 지급. 기본 36000초 = 10시간")]
    [SerializeField] private float secondsPerMaterial = 36000f;

    [Tooltip("온라인 누적 시간을 몇 초마다 체크할지")]
    [SerializeField] private float tickInterval = 1f;

    // 저장 키
    private const string MATERIAL_COUNT_KEY = "MaterialWallet_Count";
    private const string ACCUMULATED_SECONDS_KEY = "MaterialWallet_AccumulatedSeconds";
    private const string LAST_SEEN_UTC_KEY = "MaterialWallet_LastSeenUtc";

    // 보유 재료 개수
    private int _materialCount;

    // 지금까지 쌓인 시간(초). secondsPerMaterial을 채우면 1개 지급하고 0으로 리셋됨
    private float _accumulatedSeconds;

    // Start에서 구독할 때 캐싱해두고 OnDestroy에서 구독 해제할 때 이 캐시로만 접근한다.
    // StageManager.instance를 OnDestroy에서 다시 호출하면, 씬이 꺼지는 순간 이미 원본이 파괴된 뒤라
    // Singleton<T>의 "없으면 새로 만드는" 로직이 발동해서 씬 종료 직전에 새 오브젝트가 하나 생겨버림
    private StageManager _stageManager;

    // 보유 재료 개수
    public int MaterialCount => _materialCount;

    // 재료 개수가 바뀔 때마다 발생 (인자 = 변경 후 개수). UI 갱신용
    public event Action<int> MaterialCountChanged;

    protected override void Awake()
    {
        base.Awake();
        _materialCount = PlayerPrefs.GetInt(MATERIAL_COUNT_KEY, 0);
        _accumulatedSeconds = Mathf.Max(0f, PlayerPrefs.GetFloat(ACCUMULATED_SECONDS_KEY, 0f));
    }

    private void Start()
    {
        ApplyOfflineTime();

        _stageManager = StageManager.instance;
        if (_stageManager != null)
        {
            _stageManager.StageCleared += OnStageCleared;
        }

        RunPassiveTimeLoop(this.GetCancellationTokenOnDestroy()).Forget();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_stageManager != null)
        {
            _stageManager.StageCleared -= OnStageCleared;
        }

        Save();
    }

    // 앱이 완전히 꺼질 때 (빌드 기준)
    private void OnApplicationQuit()
    {
        Save();
    }

    // 모바일에서 백그라운드로 내려갈 때도 종료에 준해서 저장
    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            Save();
        }
    }

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
        Save();
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
        Save();
        return true;
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
            SaveLastSeenNow();
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
    /// </summary>
    private void ApplyOfflineTime()
    {
        string savedText = PlayerPrefs.GetString(LAST_SEEN_UTC_KEY, string.Empty);
        int materialCountBeforeOffline = _materialCount;

        if (!string.IsNullOrEmpty(savedText)
            && DateTime.TryParse(savedText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime lastSeen))
        {
            double elapsedSeconds = (DateTime.UtcNow - lastSeen).TotalSeconds;
            if (elapsedSeconds > 0d)
            {
                AccumulateSeconds((float)elapsedSeconds);
            }
        }

        SaveLastSeenNow();

        // AccumulateSeconds가 한 번 호출로 최대 1개까지만 지급하므로, 여기서 늘어난 만큼(0 또는 1)이 오프라인 지급분
        int grantedByOffline = _materialCount - materialCountBeforeOffline;

        // RewardManager(복귀 보상 팝업)는 아직 Inspector 연결이 안 끝난 상태일 수 있어서
        // instance/필드 둘 다 null 체크하고 지나감 (없어도 재료 지급 자체는 이미 끝난 뒤라 안전함)
        if (grantedByOffline > 0 && RewardManager.instance != null && RewardManager.instance.GetUpgardMaterial != null)
        {
            RewardManager.instance.GetUpgardMaterial.text = grantedByOffline.ToString();
        }
    }

    private void SaveLastSeenNow()
    {
        PlayerPrefs.SetString(LAST_SEEN_UTC_KEY, DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
    }

    private void Save()
    {
        PlayerPrefs.SetInt(MATERIAL_COUNT_KEY, _materialCount);
        PlayerPrefs.SetFloat(ACCUMULATED_SECONDS_KEY, _accumulatedSeconds);
        SaveLastSeenNow();
        PlayerPrefs.Save();
    }
}
