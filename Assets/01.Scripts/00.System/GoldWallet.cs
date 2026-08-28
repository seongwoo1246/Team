/*
게임 전체 골드 보유량. 어디서든 GoldWallet.instance 로 접근
골드는 방치형 특성상 아주 커지므로 double씀

싱글톤은 팀 공용 Singleton<T> 상속

몬스터 처치 보상은 스포너/스테이지매니저가 AddKillReward 로 지급!!(GoldGain 배율 자동 적용) 연결해야함
경제 시스템을 GameManager 쪽에서 관리하기로 하면 이 클래스만 교체하면 됩니다
*/

using System;
using UnityEngine;

/// <summary>
/// 골드 지갑 씬에 하나 두고 GoldWallet.instance 로 접근
/// </summary>
public class GoldWallet : Singleton<GoldWallet>
{
    [Header("시작 골드")]
    [Tooltip("게임 시작 시 보유 골드")]
    [SerializeField] private double startGold = 0d;

    private double _balance;

    //>현재 보유 골드
    public double Balance => _balance;

    // 골드가 바뀔 때마다 발생 (인자 = 변경 후 잔액). UI 갱신용
    public event Action<double> BalanceChanged;

    protected override void Awake()
    {
        base.Awake();
        _balance = startGold;
    }

    /// <summary>
    /// 골드를 그대로 추가한다.
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
    /// 몬스터 처치 보상을 추가한다. GoldGain 강화 배율이 자동 적용
    /// 스포너/스테이지매니저는 이 함수로 골드를 지급하면 된다
    /// </summary>
    /// <param name="baseReward">배율 적용 전 보상 골드 (Monster.RewardGold)</param>
    public void AddKillReward(double baseReward)
    {
        double multiplier = UpgradeSystem.instance != null ? UpgradeSystem.instance.GetGoldMultiplier() : 1d;
        Add(baseReward * multiplier);
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
}
