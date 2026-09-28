// 작성자: 김주연
/*
방치형 게임 특성상 스테이지가 수백개까지 갈 수 있어서, 스테이지마다 에셋 따로 안만듬
대신 "몇 스테이지부터 어떤 몬스터/보스가 풀리는지"만 정의해두고, 실제 웨이브 구성(웨이브 수,
등장 몬스터, 보스)은 전부 스테이지 번호로 계산
*/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 특정 스테이지 번호부터 등장하기 시작하는 몬스터 프리팹 하나
/// </summary>
[Serializable]
public sealed class MonsterUnlockEntry
{
    [Tooltip("등장할 몬스터 프리팹")]
    [SerializeField] private Monster monsterPrefab;

    [Tooltip("이 스테이지 번호부터 등장 시작 (1 = 처음부터)")]
    [SerializeField] private int unlockStage = 1;

    // 등장할 몬스터 프리팹
    public Monster MonsterPrefab => monsterPrefab;

    // 이 스테이지 번호부터 등장 시작
    public int UnlockStage => unlockStage;
}

/// <summary>
/// 챌린지 스테이지 전체를 관장하는 로스터.
/// 스테이지 번호를 넣으면 그 스테이지의 웨이브 수, 등장 몬스터, 보스를 계산
/// 프로젝트 창에서 우클릭 → Create → Game/Stage Roster Data 로 생성
/// </summary>
[CreateAssetMenu(fileName = "StageRosterData", menuName = "Game/Stage Roster Data", order = 3)]
public sealed class StageRosterData : ScriptableObject
{
    [Header("등장 보스")]
    [Tooltip("등장 가능한 보스들. 스테이지에서 unlockStage가 그 스테이지 이하인 것들 중에서, 10스테이지 단위로 하나씩 돌아가며 등장함")]
    [SerializeField] private List<MonsterUnlockEntry> bossMonsters = new List<MonsterUnlockEntry>();

    [Tooltip("보스가 몇 스테이지마다 바뀌는지 (예: 10 = 10스테이지마다 다음 보스로 교체)")]
    [SerializeField] private int stagesPerBossRotation = 10;

    [Header("웨이브 수 계산")]
    [Tooltip("스테이지 1의 기본 웨이브 수")]
    [SerializeField] private int baseWaveCount = 3;

    [Tooltip("웨이브 수가 1 늘어나는 스테이지 간격 (예: 10 = 10스테이지마다 웨이브 수 +1)")]
    [SerializeField] private int stagesPerWaveIncrease = 10;

    [Tooltip("웨이브 수 상한")]
    [SerializeField] private int maxWaveCount = 10;

    [Header("웨이브 구성")]
    [Tooltip("웨이브 1개에 소환할 몬스터 수")]
    [SerializeField] private int monstersPerWave = 10;

    [Tooltip("몬스터 소환 간격 (초)")]
    [SerializeField] private float spawnInterval = 0.5f;

    // 웨이브 1개에 소환할 몬스터 수
    public int MonstersPerWave => monstersPerWave;

    // 몬스터 소환 간격 (초)
    public float SpawnInterval => spawnInterval;

    /// <summary>
    /// 스테이지 번호에 따른 웨이브 수를 계산
    /// baseWaveCount에서 시작해 stagesPerWaveIncrease마다 1씩 늘고 maxWaveCount에서 멈춤
    /// </summary>
    /// <param name="stageNumber">스테이지 번호 (1 이상)</param>
    /// <returns>이 스테이지에서 진행할 웨이브 수</returns>
    public int GetWaveCount(int stageNumber)
    {
        if (stagesPerWaveIncrease <= 0)
        {
            return baseWaveCount;
        }

        int extra = (Mathf.Max(1, stageNumber) - 1) / stagesPerWaveIncrease;
        return Mathf.Min(baseWaveCount + extra, maxWaveCount);
    }

    /// <summary>
    /// 이 스테이지에서 등장할 보스를 고른다. 그 스테이지까지 잠금 해제된 보스들 중에서
    /// stagesPerBossRotation(기본 10) 스테이지 단위로 하나씩 순서대로 바뀜
    /// </summary>
    /// <param name="stageNumber">스테이지 번호</param>
    /// <returns>등장 가능한 보스가 없으면 null</returns>
    public Monster PickBossForStage(int stageNumber)
    {
        List<Monster> unlocked = new List<Monster>();
        for (int bossIndex = 0; bossIndex < bossMonsters.Count; bossIndex++)
        {
            MonsterUnlockEntry entry = bossMonsters[bossIndex];
            if (entry.MonsterPrefab != null && entry.UnlockStage <= stageNumber)
            {
                unlocked.Add(entry.MonsterPrefab);
            }
        }

        if (unlocked.Count == 0)
        {
            return null;
        }

        int rotation = stagesPerBossRotation > 0 ? stagesPerBossRotation : 1;
        int block = (Mathf.Max(1, stageNumber) - 1) / rotation;
        int index = block % unlocked.Count;
        return unlocked[index];
    }
}
