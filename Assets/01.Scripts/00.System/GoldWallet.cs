/*
게임 전체 골드 보유량. 어디서든 GoldWallet.instance 로 접근
골드는 방치형 특성상 아주 커지므로 double씀

싱글톤은 팀 공용 Singleton<T> 상속

몬스터를 직접 잡아야만 골드가 들어오던 방식은 없앴고, 대신 분당고정골드를 계속 지급함
  - 분당 골드 = baseGoldPerMinute × StageManager.ClearGoldMultiplier × (1 + 파티 장비 골드획득 보너스)
  - 게임이 꺼져있던 시간도 (최대 maxOfflineHours까지) 복귀 시 한 번에 계산해서 지급
  - 스테이지를 클리어하면 그 순간 분당 골드의 clearBonusMinutes분치를 보너스로 즉시 지급
  - GoldGain 강화 배율(UpgradeSystem)은 AddPassiveGold 안에서 자동 적용됨

경제 시스템을 GameManager 쪽에서 관리하기로 하면 이 클래스만 교체하면 됩니다
*/

using System;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 골드 지갑 씬에 하나 두고 GoldWallet.instance 로 접근
/// </summary>
public class GoldWallet : Singleton<GoldWallet>
{
    [Header("시작 골드")]
    [Tooltip("게임 시작 시 보유 골드")]
    [SerializeField] private double startGold = 0d;

    [Header("분당 골드")]
    [Tooltip("분당 기본 골드. 여기에 스테이지 클리어 배율 + 파티 장비(바지=골드획득) 보너스가 곱해져서 실제 지급량이 정해짐")]
    [SerializeField] private double baseGoldPerMinute = 10d;

    [Tooltip("골드를 몇 초마다 나눠 지급할지. 짧을수록 조금씩 부드럽게 들어옴")]
    [SerializeField] private float tickInterval = 1f;

    [Tooltip("\"마지막 확인 시각\"을 PlayerPrefs에 몇 초마다 저장해둘지 (오프라인 보상 계산용 안전장치). " +
        "너무 짧으면 매번 디스크에 강제로 쓰기 때문에 불필요하게 잦은 저장이 됨 - 이 정도 간격이면 " +
        "최악의 경우에도 이 시간만큼만 오프라인 보상을 놓칠 뿐이라 충분히 안전함")]
    [SerializeField] private float lastSeenSaveInterval = 30f;

    [Header("오프라인 보상")]
    [Tooltip("게임이 꺼져있던 시간을 최대 몇 시간까지 인정할지 (너무 오래 꺼놔도 무한정 쌓이지 않게)")]
    [SerializeField] private double maxOfflineHours = 12d;

    [Header("스테이지 클리어 보너스")]
    [Tooltip("스테이지를 클리어하면 그 순간 분당 골드의 몇 분치를 보너스로 즉시 지급할지 (180 = 3시간치)")]
    [SerializeField] private double clearBonusMinutes = 180d;

    // 마지막으로 게임이 종료(또는 확인)된 시각을 저장해두는 PlayerPrefs 키. 오프라인 보상 계산용
    private const string LAST_SEEN_UTC_KEY = "GoldWallet_LastSeenUtc";

    private double _balance;

    // 현재 보유 골드
    public double Balance => _balance;

    // 골드가 바뀔 때마다 발생 (인자 = 변경 후 잔액). UI 갱신용
    public event Action<double> BalanceChanged;

    protected override void Awake()
    {
        base.Awake();
        _balance = startGold;
    }

    private void Start()
    {
        // StageManager의 _maxClearedStage 로드(Awake)가 전부 끝난 뒤에 계산해야 정확하므로 Start에서 처리
        ApplyOfflineGold();

        if (StageManager.instance != null)
        {
            StageManager.instance.StageCleared += OnStageCleared;
        }

        RunPassiveIncomeLoop(this.GetCancellationTokenOnDestroy()).Forget();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (StageManager.instance != null)
        {
            StageManager.instance.StageCleared -= OnStageCleared;
        }

        SaveLastSeenNow();
    }

    // 앱이 완전히 꺼질 때 (에디터 정지 포함은 아님 - 빌드 기준)
    private void OnApplicationQuit()
    {
        SaveLastSeenNow();
    }

    // 모바일에서 백그라운드로 내려갈 때도 종료에 준해서 시각을 저장
    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            SaveLastSeenNow();
        }
    }

    /// <summary>
    /// 골드를 그대로 추가한다. GoldGain 강화 배율이 적용 안 된 순수 원시값을 더할 때 사용
    /// </summary>
    /// <param name="amount">추가할 양 (0 이하는 무시)</param>
    public void Add(double amount)
    {
        if (amount <= 0d)
        {
            return;
        }

        _balance += amount;
        BalanceChanged?.Invoke(_balance);
    }

    /// <summary>
    /// 골드가 충분하면 차감하고 true, 부족하면 아무 것도 안 하고 false.
    /// </summary>
    /// <param name="amount">차감할 양</param>
    /// <returns>차감 성공 여부</returns>
    public bool TrySpend(double amount)
    {
        if (amount <= 0d)
        {
            return true;
        }

        if (_balance < amount)
        {
            return false;
        }

        _balance -= amount;
        BalanceChanged?.Invoke(_balance);
        return true;
    }

    /// <summary>
    /// 분당 골드/오프라인 보상/클리어 보너스처럼, GoldGain 강화 배율을 자동 적용해야 하는
    /// 모든 골드 지급이 거쳐가는 공용 진입점
    /// </summary>
    /// <param name="baseAmount">배율 적용 전 골드</param>
    private void AddPassiveGold(double baseAmount)
    {
        double multiplier = UpgradeSystem.instance != null ? UpgradeSystem.instance.GetGoldMultiplier() : 1d;
        Add(baseAmount * multiplier);
    }

    /// <summary>
    /// 지금 이 순간 기준 분당 골드. 기본값에 스테이지 클리어 배율과 파티 장비(골드획득) 보너스를 곱한다
    /// </summary>
    private double GetCurrentGoldPerMinute()
    {
        if (StageManager.instance == null)
        {
            return baseGoldPerMinute;
        }

        double stageMultiplier = StageManager.instance.ClearGoldMultiplier;
        double equipmentBonus = StageManager.instance.PartyEquipmentGoldBonusRatio;
        return baseGoldPerMinute * stageMultiplier * (1d + equipmentBonus);
    }

    /// <summary>
    /// tickInterval마다 그 시점 분당 골드 비율만큼 나눠서 지급하는 루프
    /// lastSeenSaveInterval마다 한 번씩만 "마지막 확인 시각"도 같이 저장해둬서 강제 종료 등으로
    /// OnApplicationQuit이 안 불려도 오프라인 보상이 크게 틀어지지 않게 함
    /// (매 틱마다 저장하면 디스크에 너무 자주 쓰게 되므로 별도 주기로 나눔)
    /// </summary>
    /// <param name="token">파괴 시 루프를 멈추는 취소 토큰</param>
    private async UniTaskVoid RunPassiveIncomeLoop(CancellationToken token)
    {
        float timeSinceLastSave = 0f;

        while (!token.IsCancellationRequested)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(tickInterval), cancellationToken: token);

            double perTick = GetCurrentGoldPerMinute() * (tickInterval / 60d);
            AddPassiveGold(perTick);

            timeSinceLastSave += tickInterval;
            if (timeSinceLastSave >= lastSeenSaveInterval)
            {
                timeSinceLastSave = 0f;
                SaveLastSeenNow();
            }
        }
    }

    /// <summary>
    /// 스테이지를 클리어한 순간, 그 시점(새로 오른 배율 기준) 분당 골드의 clearBonusMinutes분치를
    /// 보너스로 즉시 지급한다. StageManager.StageCleared 구독으로 호출됨
    /// </summary>
    /// <param name="stageNumber">방금 클리어한 스테이지 번호 (여기선 안 씀 - 이벤트 시그니처 맞추기용)</param>
    private void OnStageCleared(int stageNumber)
    {
        double bonus = GetCurrentGoldPerMinute() * clearBonusMinutes;
        AddPassiveGold(bonus);
    }

    /// <summary>
    /// 마지막으로 저장해둔 시각과 지금 시각을 비교해서, 꺼져있던 시간만큼(최대 maxOfflineHours까지)
    /// 분당 골드를 한 번에 지급한다. 저장된 시각이 없으면(첫 실행) 지급 없이 지금 시각만 저장해둔다
    /// </summary>
    private void ApplyOfflineGold()
    {
        string savedText = PlayerPrefs.GetString(LAST_SEEN_UTC_KEY, string.Empty);

        if (!string.IsNullOrEmpty(savedText)
            && DateTime.TryParse(savedText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime lastSeen))
        {
            double elapsedSeconds = (DateTime.UtcNow - lastSeen).TotalSeconds;
            double cappedSeconds = Math.Max(0d, Math.Min(elapsedSeconds, maxOfflineHours * 3600d));
            double offlineMinutes = cappedSeconds / 60d;

            if (offlineMinutes > 0d)
            {
                double reward = GetCurrentGoldPerMinute() * offlineMinutes;
                AddPassiveGold(reward);
                DebugLogger<GoldWallet>.Log($"오프라인 보상 지급: {offlineMinutes:F1}분치 (최대 {maxOfflineHours}시간 인정)");
                RewardManager.instance.GetPlayerReward.text = reward.ToString();
            }
        }

        SaveLastSeenNow();
    }

    /// <summary>
    /// 지금 시각(UTC)을 오프라인 보상 계산용으로 PlayerPrefs에 저장
    /// </summary>
    private void SaveLastSeenNow()
    {
        PlayerPrefs.SetString(LAST_SEEN_UTC_KEY, DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        PlayerPrefs.Save();
    }
}
