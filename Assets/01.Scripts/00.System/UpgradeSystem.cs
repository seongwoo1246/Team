/*
파티 강화 시스템.
트랙 레벨은 파티 공용이라, 강화하면 모든 캐릭터의 해당 스탯이 동시에 오릅니다

트랙별 비용:  base × (growth ^ 현재 트랙레벨)   ← GameConfig 에서 base/growth 를 읽음
강화 상한 없음 (Crit 트랙은 확률이 100%에서 멈추므로 사실상 유한).

UI 의 강화 버튼이 UpgradeSystem.instance.TryUpgrade(UpgradeTrack.Power) 식으로 호출
싱글톤은 팀 공용 Singleton<T>를 상속
*/

using System;
using UnityEngine;

// 파티 강화 시스템. 씬에 하나 두고 UpgradeSystem.instance 로 접근
public class UpgradeSystem : Singleton<UpgradeSystem>
{
    [Header("설정")]
    [Tooltip("트랙별 강화 비용 파라미터. CSV 임포터가 만든 GameConfig 에셋을 넣는다")]
    [SerializeField] private GameConfig config;

    // 인덱스 = (int)UpgradeTrack (Power=0 ... AttackSpeed=5)
    private readonly int[] _levels = new int[System.Enum.GetValues(typeof(UpgradeTrack)).Length];

    //트랙이 강화되면 발생 (인자 = 강화된 트랙). 캐릭터·UI가 구독해 갱신한다
    public event Action<UpgradeTrack> TrackUpgraded;

    // 현재 트랙 레벨 (0부터 시작)
    public int GetLevel(UpgradeTrack track)
    {
        return _levels[(int)track];
    }

    // 이 트랙을 다음 레벨로 올리는 데 필요한 골드
    public double GetCost(UpgradeTrack track)
    {
        if (config == null)
        {
            return 0d;
        }

        return StatCalculator.GetUpgradeCost(
            config.GetUpgradeBaseCost(track),
            config.GetUpgradeGrowth(track),
            _levels[(int)track]);
    }

    // 골드가 충분하면 트랙을 1레벨 올리고 true. 부족하거나 설정이 없으면 false.
    public bool TryUpgrade(UpgradeTrack track)
    {
        if (config == null)
        {
            DebugLogger<UpgradeSystem>.LogWarning("GameConfig 가 지정되지 않음 - 강화 불가");
            return false;
        }

        double cost = GetCost(track);
        if (GoldWallet.instance == null || !GoldWallet.instance.TrySpend(cost))
        {
            return false;
        }

        _levels[(int)track]++;
        TrackUpgraded?.Invoke(track);
        return true;
    }

    // 현재 GoldGain 트랙 레벨 기준 골드 획득 배율. (1.0 = 기본, 1.2 = +20%)
    public double GetGoldMultiplier()
    {
        if (config == null)
        {
            return 1d;
        }
        return 1d + (config.GoldGainPerLevel * _levels[(int)UpgradeTrack.GoldGain]);
    }

    /// <summary>
    /// 현재 AttackSpeed 트랙 레벨 기준 공격 속도 계수. 공격 간격을 이 값으로 나눈다.
    /// (1.0 = 기본, 2.0 = 2배 빠름)
    /// </summary>
    public float GetAttackSpeedFactor()
    {
        if (config == null)
        {
            return 1f;
        }
        return 1f + (config.AttackSpeedPerLevel * _levels[(int)UpgradeTrack.AttackSpeed]);
    }
}
