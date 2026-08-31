using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/*
 내가 만들어 본 버전은 

파밍에서 연출만 보여주고 시간당 금액이 쌓이다가 한번에 얻어가는 형태이다.
챌린지 스테이지에서는 N개를 시간 안에 다 못잡거나 죽으면 실패하는 형태로 만들어 봤다. 기본형은 3개이다.

 */



[Serializable]
public class WaveData
{
    public MonsterKind[] monsterKinds;
    public int monsterType = 0;

}

[Serializable]
public class StageData
{
    public int stageNumber;
    public WaveData[] waveDatas;
    public float timeLimit = 60f;
    public float goldPerSecondReward = 10f;

}


/// <summary>
/// 스테이지의 흐름  웨이브 관리등을 할 클래스
/// 방치에서는 실제의 싸움은 없고 나와서 싸우고 쓰러지는 연출만으로 진행 할 예정 
/// 챌린지에서는 실제의 싸움이 일어나고 웨이브는 3개 구성에 클리어 시간을 제한 해서 클리어 여부 결정
/// </summary>
public class StageManagerSeongWoo : Singleton<StageManagerSeongWoo>
{
    
    public int HighestCleardeStage { get; private set; } = 0;

    private StageMode stageMode = StageMode.Farming;
    public StageMode CurrentMode => stageMode;

    [Header("Farming Settings")]
    [SerializeField] private float farmingAttackDuration = 1.8f;
    [SerializeField] private float farmingDieDuration = 1.2f;
    [SerializeField] private float farmingGoldInterval = 60f;   // 1분
    [SerializeField] private int farmingGoldPerInterval = 50;

    [Header("Challenge Settings")]
    [SerializeField] private List<StageData> stageDataList;

    private int currentStageIndex = 0;
    private CancellationTokenSource ModeCancel;

    // UI/사운드/이펙트가 구독할 이벤트. 매니저가 UI를 직접 참조하지 않게 해서 결합도를 낮춘다.
    public event Action<int> OnStageCleared;
    public event Action<int> OnStageFailed;
    public event Action<float> OnStageTimeChanged;
    public event Action OnReturnedToFarming;

    private void Start()
    {
        EnterFarmingMode();
    }

    private void OnDestroy()
    {
        ModeCancel?.Cancel();
        ModeCancel?.Dispose();
    }

    public void EnterFarmingMode()
    {
        ChangeMode(StageMode.Farming, FarmingRoutine);
    }

    public void EnterChallengeMode(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= stageDataList.Count)
        {
            Debug.LogWarning($"[StageManagerSeongWoo] 잘못된 스테이지 인덱스: {stageIndex}");
            return;
        }

        currentStageIndex = stageIndex;
        //ChangeMode(StageMode.Challenge, ct => ChallengeRoutine(targetStage,ct));
    }


    private void ChangeMode(StageMode mode, Func<CancellationToken , UniTask> routineFactory)
    {
        ModeCancel?.Cancel();
        ModeCancel?.Dispose();
        ModeCancel = new CancellationTokenSource();

        stageMode = mode;
        routineFactory(ModeCancel.Token).Forget();
    }

    private async UniTask FarmingRoutine( CancellationToken ct)
    {
        float goldTimer = 0f;

        // while(true) 대신 CurrentMode를 조건으로 걸어서, 외부에서 EnterChallengeMode()가
        // 호출되어 모드가 바뀌면 이 루프가 스스로 종료 조건을 인지하게 한다.
        // (실제로는 ChangeMode에서 StopCoroutine으로 강제 종료되지만, 안전장치로 이중 방어)
        while (stageMode == StageMode.Farming)
        {
           // GameObject Pixel = ObjcetPoolManager.instance.Spawn<GameObject>(enumType.Pixel);
           // GameObject Cartoon = ObjcetPoolManager.instance.Spawn<GameObject>(enumType.Cartoon);

            //Pixel.GetComponent<Animator>()?.SetTrigger("Attack");
            await UniTask.Delay(TimeSpan.FromSeconds(farmingAttackDuration), cancellationToken: ct);

            // 몬스터 사망 연출 + 캐릭터가 좋아하는(승리) 연출
            //Cartoon.GetComponent<Animator>()?.SetTrigger("Die");
            //Pixel.GetComponent<Animator>()?.SetTrigger("Cheer");
            await UniTask.Delay(TimeSpan.FromSeconds(farmingDieDuration), cancellationToken: ct);

            // Destroy가 아니라 풀로 반환. 파밍은 무한 반복이라 Instantiate/Destroy를 매번 하면
            // GC 부하와 프레임 드랍이 누적되기 때문에 오브젝트 풀링이 사실상 필수.
            //ObjcetPoolManager.instance.Despawn(enumType.Pixel, Pixel);
            //ObjcetPoolManager.instance.Despawn(enumType.Cartoon , Cartoon);

            goldTimer += farmingAttackDuration + farmingDieDuration;
            if (goldTimer >= farmingGoldInterval)
            {
                goldTimer -= farmingGoldInterval; // 초과분 이월 (타이머 오차 누적 방지)
                GoldWallet.instance.Add(farmingGoldPerInterval);
            }
        }
    }

    // ---------------- 챌린지 모드 ----------------

    private async UniTask ChallengeRoutine(StageData stage , CancellationToken ct)
    {
        float remainingTime = stage.timeLimit;

        for (int waveIndex = 0; waveIndex < stage.waveDatas.Length; waveIndex++)
        {
            List<GameObject> aliveMonsters = SpawnWave(stage.waveDatas[waveIndex]);

            // "몬스터가 전부 죽었는가"와 "시간이 남았는가"를 같은 while문 안에서 매 프레임 검사.
            // 코루틴을 두 개(타이머용/전투용)로 나누지 않고 하나로 합쳐서 동기화 문제를 없앴다.
            while (aliveMonsters.Exists(m => m != null && m.activeInHierarchy))
            {
                remainingTime -= Time.deltaTime;
                OnStageTimeChanged?.Invoke(remainingTime);

                if (remainingTime <= 0f)
                {
                    OnStageFailed?.Invoke(stage.stageNumber);
                    EnterFarmingMode();
                    return; // 실패 즉시 이 코루틴을 완전히 종료 (아래 클리어 로직으로 안 넘어가게)
                }


                await UniTask.Yield(PlayerLoopTiming.Update,ct);
                aliveMonsters.RemoveAll(m => m == null || !m.activeInHierarchy);
              
            }
        }

        // 웨이브 3개를 모두 통과 = 클리어
        if (stage.stageNumber > HighestCleardeStage)
        {
            HighestCleardeStage = stage.stageNumber; // 현재값과 비교해서 최고기록만 갱신
        }

        OnStageCleared?.Invoke(stage.stageNumber);
        // 이후 진행(다음 스테이지 / 파밍 복귀)은 UI 버튼이 아래 두 함수를 호출할 때까지 대기.
        // 매니저가 스스로 판단하지 않고 플레이어 입력을 기다리는 것이 챌린지 모드의 핵심 요구사항.
    }

    private List<GameObject> SpawnWave(WaveData wave)
    {
        var spawned = new List<GameObject>();
        foreach (var type in wave.monsterKinds)
        {
            for (int i = 0; i < wave.monsterType; i++)
            {
                //spawned.Add(ObjcetPoolManager.instance.Spawn<GameObject>(type));
            }
        }
        return spawned;
    }

    // ---------------- 클리어 후 선택지 (UI 버튼에서 호출) ----------------

    public void ProceedToNextStage()
    {
        int nextIndex = currentStageIndex + 1;
        if (nextIndex < stageDataList.Count)
        {
            EnterChallengeMode(nextIndex);
        }
        else
        {
            EnterFarmingMode(); // 마지막 스테이지라면 파밍으로 복귀
        }
    }

    public void ReturnToFarming()
    {
        OnReturnedToFarming?.Invoke();
        EnterFarmingMode();
    }
}


/*
아래는 이 스크립트가 의존하는 외부 타입들의 최소 형태 예시입니다.
실제 프로젝트의 Singleton.cs / ObjcetPoolManager.cs / GoldWallet.cs 구조에 맞춰
메서드 시그니처(Spawn/Despawn/AddGold 등)만 맞춰주시면 그대로 연결됩니다.

public enum PoolType { Character, Monster }

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance => instance;
    protected virtual void Awake()
    {
        if (instance == null) { instance = this as T; }
        else { Destroy(gameObject); }
    }
}
*/




/*
 
스테이지 매니저를 만들 때 필요한 것들 

파밍모드

 전투 장면만 연출
애니메이션만 연출 3초정도 뒤에 몬스터 죽는 연출 나오고 비활성화 및 초기화 그리고 반복
1분당 골드 저장



챌린지 모드

스테이지 웨이브 마다 나올 몬스터 enum을 표현
스테이지 넘버 현재 == 최고 숫자를 갱신
스테이지 간격 클리어 후 다음 스테이지로 갈지 파밍모드로 돌릴지 결정
스테이지 시간제한 30초 혹은 일정 시간안에 스테이지 클리어 못하면 실패
스테이지 클리어시 시간당 획득 재화량 증가


 */
