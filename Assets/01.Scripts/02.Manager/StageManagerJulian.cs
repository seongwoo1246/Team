/*
스테이지 흐름을 관리하는 클래스.
메인 화면 파밍(Farming) ↔ 챌린지 스테이지(Challenge) 두모드를 오가며
챌린지 스테이지를 클리어할 때마다 파밍 골드 획득 배율이 영구히 오름

파밍  : 몬스터 체력 관리 없이 무한 사냥 (그냥 계속 소환됨)
챌린지: 스테이지 번호를 StageRosterData에 넣어 웨이브 수/등장 몬스터/보스를 계산하고,
        웨이브를 순서대로 전멸시킨 뒤 보스를 잡으면 클리어 → 파밍으로 복귀
        (스테이지가 수백 개 있어도 에셋을 스테이지마다 안 만들어도 되도록 전부 수식으로 계산함)

보상은 GoldWallet.AddKillReward로 지급한다 (UpgradeSystem의 GoldGain 배율은 그 안에서 자동 적용됨).
스테이지 클리어 배율(ClearGoldMultiplier)은 여기서 별도로 곱해서 넘김!
*/

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 스테이지 흐름(파밍 ↔ 챌린지)을 관리하는 매니저. 씬에 하나 두고 StageManagerJulian.instance로 접근
/// </summary>
public sealed class StageManagerJulian : Singleton<StageManagerJulian>
{
    [Header("스포너")]
    [Tooltip("몬스터를 실제로 소환할 스포너")]
    [SerializeField] private MonsterSpawnerJulian spawner;

    [Header("파밍 설정")]
    [Tooltip("메인 화면에서 무작위로 소환할 몬스터 프리팹들")]
    [SerializeField] private Monster[] farmingMonsters;

    [Tooltip("파밍 몬스터 소환 간격 (초)")]
    [SerializeField] private float farmingSpawnInterval = 1f;

    [Tooltip("파밍 몬스터 최소 레벨. 클리어한 최대 스테이지가 이보다 높아지면 그 스테이지 레벨을 대신 씀")]
    [SerializeField] private int farmingMonsterLevel = 1;

    [Tooltip("파밍 몬스터가 동시에 존재할 수 있는 최대 마리 수")]
    [SerializeField] private int maxFarmingMonsterCount = 5;

    [Header("챌린지 스테이지")]
    [Tooltip("스테이지 번호별 웨이브 구성/등장 몬스터/보스를 계산해주는 로스터")]
    [SerializeField] private StageRosterData roster;

    [Header("파티")]
    [Tooltip("파밍 복귀 시 전원 부활시키고, 챌린지 중 전멸 여부를 판정할 파티 캐릭터들")]
    [SerializeField] private CharacterBase[] party;

    [Header("클리어 보상")]
    [Tooltip("클리어한 스테이지 1개당 파밍 골드 획득 배율 증가율 (복리). 0.03 = 스테이지당 ×1.03배씩 누적")]
    [SerializeField] private double goldMultiplierPerClearedStage = 0.03d;

    // 현재 진행 모드
    private StageMode _currentMode = StageMode.Farming;

    // 지금까지 클리어한 최대 스테이지 번호. 0 = 아직 클리어한 스테이지 없음
    private int _maxClearedStage = 0;

    // 현재 진행 중인 흐름(파밍 루프 / 챌린지 루프)을 멈추는 토큰
    private CancellationTokenSource _flowCts;

    // 챌린지 진행 중, 현재 웨이브에 살아있는 몬스터 수
    private int _aliveInWave;

    // 파밍 진행 중, 현재 필드에 살아있는 파밍 몬스터 수
    private int _aliveInFarming;

    // 현재 진행 모드
    public StageMode CurrentMode => _currentMode;

    // 지금까지 클리어한 최대 스테이지 번호
    public int MaxClearedStage => _maxClearedStage;

    // 클리어한 최대 스테이지에 비례한 영구 골드 배율 (복리). 1.0 = 기본(아직 클리어한 스테이지 없음)
    // 파밍 몬스터 레벨도 maxClearedStage로 지수 성장하기 때문에, 이 배율도 선형이 아니라 복리로 둬야
    // 스테이지가 쌓여도 괜찮음
    public double ClearGoldMultiplier => System.Math.Pow(1d + goldMultiplierPerClearedStage, _maxClearedStage);

    private void Start()
    {
        EnterFarming();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _flowCts?.Cancel();
        _flowCts?.Dispose();
    }

    /// <summary>
    /// 메인 화면 파밍 모드로 진입한다. 체력 관리 없이 무한 사냥하며 골드를 번다
    /// 챌린지 스테이지를 클리어하면 자동으로 여기로 돌아온다
    /// </summary>
    public void EnterFarming()
    {
        RestartFlow();
        _currentMode = StageMode.Farming;
        RevivePartyIfNeeded();
        RunFarmingLoopAsync(_flowCts.Token).Forget();
    }

    /// <summary>
    /// 지정한 스테이지 번호의 챌린지로 진입한다. 스테이지 선택 버튼에서 호출
    /// 웨이브 구성/등장 몬스터/보스는 roster가 스테이지 번호로 계산
    /// </summary>
    /// <param name="stageNumber">진행할 스테이지 번호 (1 이상)</param>
    public void EnterChallenge(int stageNumber)
    {
        if (roster == null || stageNumber < 1)
        {
            DebugLogger<StageManagerJulian>.LogWarning($"잘못된 챌린지 진입 요청 (stageNumber: {stageNumber})");
            return;
        }

        if (roster.PickStrongestBoss(stageNumber) == null)
        {
            DebugLogger<StageManagerJulian>.LogWarning($"스테이지 {stageNumber}에 등장 가능한 보스가 없어 챌린지를 시작하지 않음 (roster의 bossMonsters 설정 확인)");
            return;
        }

        RestartFlow();
        _currentMode = StageMode.Challenge;
        RunChallengeLoopAsync(stageNumber, _flowCts.Token).Forget();
    }

    /// <summary>
    /// 진행 중이던 흐름을 멈추고, 이전 모드에서 남아있던 몬스터를 전부 회수한 뒤
    /// 새 흐름을 위한 취소 토큰을 새로 만든다
    /// </summary>
    private void RestartFlow()
    {
        _flowCts?.Cancel();
        _flowCts?.Dispose();
        _flowCts = new CancellationTokenSource();

        spawner.DespawnAll();
    }

    /// <summary>
    /// 파밍 루프. 취소될 때까지 farmingMonsters 중 하나를 무작위로 계속 소환
    /// </summary>
    /// <param name="token">챌린지 진입 등으로 파밍을 멈출 때 쓰는 취소 토큰</param>
    private async UniTaskVoid RunFarmingLoopAsync(CancellationToken token)
    {
        if (farmingMonsters == null || farmingMonsters.Length == 0)
        {
            DebugLogger<StageManagerJulian>.LogWarning("파밍 몬스터 프리팹이 비어있음");
            return;
        }

        _aliveInFarming = 0;

        while (!token.IsCancellationRequested)
        {
            if (_aliveInFarming < maxFarmingMonsterCount)
            {
                Monster prefab = farmingMonsters[UnityEngine.Random.Range(0, farmingMonsters.Length)];
                int level = Mathf.Max(farmingMonsterLevel, _maxClearedStage);
                // 파밍은 캐릭터 체력을 관리하지 않으므로 harmless: true로 소환 (공격은 하되 실제 피해 없음)
                Monster monster = spawner.Spawn(prefab, level, harmless: true);
                if (monster != null)
                {
                    _aliveInFarming++;
                    monster.Died += OnFarmingMonsterDied;
                }
            }

            await UniTask.Delay(TimeSpan.FromSeconds(farmingSpawnInterval), cancellationToken: token);
        }
    }

    /// <summary>
    /// 챌린지 루프. roster가 계산해주는 웨이브 수만큼 웨이브를 전멸시키고, 마지막에 보스를 잡으면 클리어 처리
    /// </summary>
    /// <param name="stageNumber">진행할 스테이지 번호</param>
    /// <param name="token">파밍 복귀 등으로 챌린지를 멈출 때 쓰는 취소 토큰</param>
    private async UniTaskVoid RunChallengeLoopAsync(int stageNumber, CancellationToken token)
    {
        int waveCount = roster.GetWaveCount(stageNumber);
        for (int waveIndex = 0; waveIndex < waveCount; waveIndex++)
        {
            bool waveCleared = await RunWaveAsync(stageNumber, token);
            if (!waveCleared)
            {
                RetreatFromWipe();
                return;
            }
        }

        bool bossDefeated = await RunBossAsync(stageNumber, token);
        if (bossDefeated)
        {
            OnStageCleared(stageNumber);
        }
        else if (IsPartyWiped())
        {
            RetreatFromWipe();
        }
    }

    /// <summary>
    /// 웨이브 하나를 진행한다. roster.SpawnInterval마다 몬스터를 소환하고, 전부 처치될 때까지 대기
    /// 도중에 파티가 전멸하면 남은 소환을 멈추고 즉시 중단
    /// </summary>
    /// <returns>웨이브를 끝까지 클리어했으면 true, 파티가 전멸해서 중단됐으면 false</returns>
    private async UniTask<bool> RunWaveAsync(int stageNumber, CancellationToken token)
    {
        _aliveInWave = 0;

        for (int spawnIndex = 0; spawnIndex < roster.MonstersPerWave; spawnIndex++)
        {
            if (IsPartyWiped())
            {
                return false;
            }

            Monster prefab = roster.PickRandomNormalMonster(stageNumber);
            if (prefab == null)
            {
                DebugLogger<StageManagerJulian>.LogWarning($"스테이지 {stageNumber}에 등장 가능한 일반 몬스터가 없음 (roster의 normalMonsters 설정 확인)");
            }

            // 챌린지는 진짜 전투이므로 harmless: false (실제 피해가 들어감)
            Monster monster = spawner.Spawn(prefab, stageNumber, harmless: false);
            if (monster != null)
            {
                _aliveInWave++;
                monster.Died += OnWaveMonsterDied;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(roster.SpawnInterval), cancellationToken: token);
        }

        await UniTask.WaitUntil(() => _aliveInWave <= 0 || IsPartyWiped(), cancellationToken: token);

        return !IsPartyWiped();
    }

    /// <summary>
    /// 이 스테이지에서 등장 가능한 가장 강한 보스를 소환하고 처치될 때까지 대기
    /// </summary>
    /// <returns>보스를 실제로 잡았으면 true. 보스가 없어서 시작도 못 했으면 false</returns>
    private async UniTask<bool> RunBossAsync(int stageNumber, CancellationToken token)
    {
        Monster bossPrefab = roster.PickStrongestBoss(stageNumber);
        Monster boss = spawner.Spawn(bossPrefab, stageNumber, harmless: false);
        if (boss == null)
        {
            DebugLogger<StageManagerJulian>.LogWarning($"스테이지 {stageNumber}에서 등장 가능한 보스가 없음 - 클리어 처리하지 않음");
            return false;
        }

        bool bossDefeated = false;

        void OnBossDied(Monster deadBoss)
        {
            bossDefeated = true;
            GrantKillReward(deadBoss.RewardGold);
        }

        boss.Died += OnBossDied;

        await UniTask.WaitUntil(() => bossDefeated || IsPartyWiped(), cancellationToken: token);

        boss.Died -= OnBossDied;

        return bossDefeated;
    }

    /// <summary>
    /// 파밍 중 소환된 몬스터가 죽었을 때 보상을 지급
    /// </summary>
    /// <param name="monster">죽은 몬스터</param>
    private void OnFarmingMonsterDied(Monster monster)
    {
        monster.Died -= OnFarmingMonsterDied;
        _aliveInFarming--;
        GrantKillReward(monster.RewardGold);
    }

    /// <summary>
    /// 웨이브 중 소환된 몬스터가 죽었을 때 남은 수를 줄이고 보상을 지급
    /// </summary>
    /// <param name="monster">죽은 몬스터</param>
    private void OnWaveMonsterDied(Monster monster)
    {
        monster.Died -= OnWaveMonsterDied;
        _aliveInWave--;
        GrantKillReward(monster.RewardGold);
    }

    /// <summary>
    /// 파티 중 죽어있는 캐릭터를 전부 되살린다. 파밍 복귀(EnterFarming) 시마다 호출해서
    /// 챌린지에서 죽고 온 캐릭터도 안전한 파밍 화면에서는 항상 전원 활동 상태가 되게
    /// </summary>
    private void RevivePartyIfNeeded()
    {
        if (party == null)
        {
            return;
        }

        for (int partyIndex = 0; partyIndex < party.Length; partyIndex++)
        {
            CharacterBase character = party[partyIndex];
            if (character != null && character.IsDead)
            {
                character.Revive();
            }
        }
    }

    /// <summary>
    /// 파티 전원이 죽었는지 확인한다. party가 비어있으면 전멸 판정을 하지 않는다(항상 false)
    /// </summary>
    private bool IsPartyWiped()
    {
        if (party == null || party.Length == 0)
        {
            return false;
        }

        for (int partyIndex = 0; partyIndex < party.Length; partyIndex++)
        {
            CharacterBase character = party[partyIndex];
            if (character != null && !character.IsDead)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 파티 전멸로 챌린지를 더 진행할 수 없을 때, 챌린지를 중단하고 파밍으로 후퇴
    /// (EnterFarming이 파티도 다시 부활시켜준다)
    /// </summary>
    private void RetreatFromWipe()
    {
        DebugLogger<StageManagerJulian>.LogWarning("파티 전멸로 챌린지를 중단하고 파밍으로 후퇴함");
        EnterFarming();
    }

    /// <summary>
    /// 처치 보상을 골드로 지급한다. 스테이지 클리어 배율을 곱한 뒤 GoldWallet에 넘기면
    /// GoldWallet 안에서 UpgradeSystem의 GoldGain 배율이 추가로 자동 적용
    /// </summary>
    /// <param name="baseReward">몬스터의 기본 보상 골드 (Monster.RewardGold)</param>
    private void GrantKillReward(double baseReward)
    {
        if (GoldWallet.instance == null)
        {
            return;
        }

        GoldWallet.instance.AddKillReward(baseReward * ClearGoldMultiplier);
    }

    /// <summary>
    /// 스테이지 클리어 처리. 최고 기록을 갱신하고 파밍 모드로 복귀
    /// </summary>
    /// <param name="stageNumber">방금 클리어한 스테이지 번호</param>
    private void OnStageCleared(int stageNumber)
    {
        if (stageNumber > _maxClearedStage)
        {
            _maxClearedStage = stageNumber;
        }

        EnterFarming();
    }
}
