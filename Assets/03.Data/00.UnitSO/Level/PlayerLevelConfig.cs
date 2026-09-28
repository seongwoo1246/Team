using System;
using System.Collections.Generic;
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

[CreateAssetMenu(fileName = "PlayerLevelConfig", menuName = "Game/Player Level Config", order = 11)]
public sealed class PlayerLevelConfig : ScriptableObject
{
    [Header("레벨 구간")]
    [SerializeField] private List<ExpTier> tiers = new List<ExpTier>();

    [Header("분당 경험치")]
    [SerializeField] private float baseExpPerMinute = 20f;
    [SerializeField] private float tickInterval = 1f;

    [Header("오프라인 보상")]
    [SerializeField] private double maxOfflineHours = 12d;

    public IReadOnlyList<ExpTier> Tiers => tiers;
    public float BaseExpPerMinute => baseExpPerMinute;
    public float TickInterval => tickInterval;
    public double MaxOfflineHours => maxOfflineHours;
}
