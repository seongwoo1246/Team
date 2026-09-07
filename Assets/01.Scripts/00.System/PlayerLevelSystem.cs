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

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 레벨 구간 1개. 이 구간(startLevel부터)에서는 레벨업 1회에 expPerLevelUp만큼의 경험치가 고정으로 든다
/// </summary>
[Serializable]
public sealed class ExpTier
{
    [Tooltip("이 구간이 시작하는 레벨 (예: 101 = 101레벨부터 이 구간 적용)")]
    [SerializeField] private int startLevel = 1;

    [Tooltip("이 구간에서 레벨업 1회당 고정으로 필요한 경험치")]
    [SerializeField] private float expPerLevelUp = 20f;

    // 이 구간이 시작하는 레벨
    public int StartLevel => startLevel;

    // 이 구간의 레벨업 1회당 필요 경험치
    public float ExpPerLevelUp => expPerLevelUp;
}

/// <summary>
/// 플레이어 레벨/경험치 관리 씬에 하나 두고 PlayerLevelSystem.instance로 접근
/// </summary>
public sealed class PlayerLevelSystem : Singleton<PlayerLevelSystem>
{
    // 구간 목록이 비어있을 때(설정 안 됐을 때) 쓰는 대체값
    private const float DEFAULT_EXP_PER_LEVEL = 20f;

    [Header("레벨 구간")]
    [Tooltip("레벨 구간 목록. 반드시 startLevel 오름차순으로 넣어야 함 (예: 1, 101, 301, 501, 1001 순서)")]
    [SerializeField] private List<ExpTier> tiers = new List<ExpTier>();

    [Header("분당 경험치")]
    [Tooltip("분당 획득 경험치 (고정값). 골드와 달리 스테이지 진행 배율을 일부러 안 태움 - " +
        "레벨이 강화 상한으로서 독립적으로 작동하게 하기 위함")]
    [SerializeField] private float baseExpPerMinute = 20f;

    [Tooltip("경험치를 몇 초마다 나눠 지급할지")]
    [SerializeField] private float tickInterval = 1f;

    [Tooltip("레벨/경험치를 PlayerPrefs에 몇 초마다 저장해둘지 (앱 강제종료 대비 안전장치)")]
    [SerializeField] private float saveInterval = 30f;

    // 저장 키
    private const string LEVEL_KEY = "PlayerLevelSystem_Level";
    private const string EXP_KEY = "PlayerLevelSystem_Exp";

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

    protected override void Awake()
    {
        base.Awake();

        // 다른 시스템(UpgradeSystem 등)이 Start에서 이 값을 참조할 수 있으므로 Awake에서 먼저 로드
        _level = Mathf.Max(1, PlayerPrefs.GetInt(LEVEL_KEY, 1));
        _currentExp = Mathf.Max(0f, PlayerPrefs.GetFloat(EXP_KEY, 0f));
    }

    private void Start()
    {
        RunPassiveExpLoop(this.GetCancellationTokenOnDestroy()).Forget();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Save();
    }

    // 앱이 완전히 꺼질때 (빌드 기준)
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
    /// tickInterval마다 분당 경험치만큼 나눠서 지급하는 루프. saveInterval마다 한 번씩 저장도 같이함
    /// </summary>
    /// <param name="token">파괴 시 루프를 멈추는 취소 토큰</param>
    private async UniTaskVoid RunPassiveExpLoop(CancellationToken token)
    {
        float timeSinceLastSave = 0f;

        while (!token.IsCancellationRequested)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(tickInterval), cancellationToken: token);

            float perTick = baseExpPerMinute * (tickInterval / 60f);
            AddExp(perTick);

            timeSinceLastSave += tickInterval;
            if (timeSinceLastSave >= saveInterval)
            {
                timeSinceLastSave = 0f;
                Save();
            }
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

        if (leveledUp)
        {
            LevelUp?.Invoke(_level);
            Save();
        }
    }

    /// <summary>
    /// 지정한 레벨에서 레벨업 1회에 필요한 경험치를 구간 목록에서 찾아 돌려줌
    /// 구간 목록이 비어있으면 DEFAULT_EXP_PER_LEVEL을 대신씀
    /// </summary>
    /// <param name="level">기준 레벨</param>
    private float GetRequiredExp(int level)
    {
        float result = DEFAULT_EXP_PER_LEVEL;

        for (int i = 0; i < tiers.Count; i++)
        {
            if (tiers[i].StartLevel <= level)
            {
                result = tiers[i].ExpPerLevelUp;
            }
            else
            {
                break;
            }
        }

        return result;
    }

    /// <summary>
    /// 레벨/경험치를 PlayerPrefs에 저장
    /// </summary>
    private void Save()
    {
        PlayerPrefs.SetInt(LEVEL_KEY, _level);
        PlayerPrefs.SetFloat(EXP_KEY, _currentExp);
        PlayerPrefs.Save();
    }
}
