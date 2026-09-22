using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;


//담당자 - 정성우



/// <summary>
/// 업적 관련 정보를 담고 있는 클래스
/// </summary>
[System.Serializable]
public class Achievement
{
    // 업적 아이디
    // (현재는 int긴 한데 너무 많아지면 enum으로 분류할 예정)
    public int id;
    // 업적 이름
    public string title;
    // 업적 달성 목표 수치
    public double targetProgress;
    // 업적 현재 달성율 
    public double currentProgress;
    // 업적 달성 여부
    public bool isUnLocked;
    // 업적 클리어시 보상 종류
    public RewardType rewardType;
    //보상 수량
    public double rewardAmount;
    // 보상 수령 여부
    public bool isClaimed;

    public Achievement(int id, string title, double targetProgress, double currentProgress, bool isUnLocked, RewardType rewardType, double rewardAmount, bool isClaimed)
    {
        this.id = id;
        this.title = title;
        this.targetProgress = targetProgress;
        this.currentProgress = 0;
        this.isUnLocked = false;
        this.rewardType = rewardType;
        this.rewardAmount = rewardAmount;
        this.isClaimed = false;
    }
}

/// <summary>
/// json에 저장 하기 위해 필요한 거만 뽑아낸 클래스
/// </summary>
[System.Serializable]
public class AchievementSaveData
{
    public int id;
    public double currentProgress;
    public bool isClaimed;
    public bool isUnLocked;
}

#region 업적 관련 이벤트 함수 모음
/// <summary>
/// 옵저버 패턴을 이용한 글로벌 이벤트 발행기 (게임코드와 업적코드를 연결해주는 역할)
/// </summary>
public static class GameEvents
{
    // 몬스터를 잡으면 혹은 보스를 잡으면 발생할 이벤트
    public static event Action OnEnemyKilled;
    // 돈을 받거나 얻으면 발생하는 이벤트
    public static event Action<double> OnGoldObtained;
    // 스테이지 클리어시 발생하는 이벤트
    public static event Action OnStageCleared;
    // 로그인 하고 로그 아웃한 시간을 구해서 얼마나 플레이하는검사할 때 할 이벤트
    public static event Action<double> OnPlayTime;
   
    



    public static void TriggerOnEnemyKilled() => OnEnemyKilled?.Invoke();
    public static void TriggerOnGoldObtained(double amount) => OnGoldObtained?.Invoke(amount);
    public static void TriggerOnStageCleared() => OnStageCleared?.Invoke();
    public static void TriggerOnPlayTime(double times) => OnPlayTime?.Invoke(times);
    

}
#endregion

/// <summary>
/// 업적 데이터를 로컬과 서버와 동기화 하여 관리하는 스크립트
/// </summary>
public class AchievementManager : Singleton<AchievementManager> , ILoadable
{
    [SerializeField] private AchievementSlot slot;
    [SerializeField] private Transform contentParent;

    [SerializeField] private GameObject BackGround;

    [SerializeField] private Button openBtn;
    [SerializeField] private Button closeBtn;

    // 업적을 담아두는 리스트와 딕셔너리
    public List<Achievement> achievements = new List<Achievement>();
    public Dictionary<int, Achievement> achievementsDictionary = new Dictionary<int, Achievement>();
    public List<AchievementSlot> activeSlots = new List<AchievementSlot>();

    //업적 슬롯보다는 빨리 되어야함
    //오브젝트 풀링 보다는 늦어야함
    public int LoadOrder => 30;

    protected override void Awake()
    {
        base.Awake();
        InitializeDictionary();
        gameObject.SetActive(false);
        
        if (ObjcetPoolManager.Instance != null )
        {
            ObjcetPoolManager.Instance.RegisterPool<AchievementSlot>(enumType.UI, slot, 4);
        }
        
        if(openBtn != null)
        {

            openBtn.onClick.RemoveAllListeners();
            openBtn.onClick.AddListener(OpenUI);
            openBtn.gameObject.SetActive(true);
        }
        if(closeBtn != null)
        {
            closeBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.AddListener(closeUI);
           
        }

        
    }

    // UI 슬롯들이 구독할 전용 이벤트 (변경된 업적의 id를 전달 )
    public static event System.Action<int> OnAchievementUpdated;


    public void OpenUI()
    {
        gameObject.SetActive(true);
        if(BackGround != null) BackGround.gameObject.SetActive(true);
        if(closeBtn != null) closeBtn.gameObject.SetActive(true);

        ClearActiveSlots();

        foreach (var ach  in achievements)
        {
            AchievementSlot newslot = ObjcetPoolManager.Instance.Spawn<AchievementSlot>(enumType.UI);


            if (newslot != null)
            {
               
                newslot.transform.SetParent(contentParent, false);
                newslot.BindData(ach);

                activeSlots.Add(newslot);
            }
        }

      
    }


    public void closeUI()
    {
        ClearActiveSlots();
        slot.gameObject.SetActive(false);
        BackGround.gameObject.SetActive(false);
    }

    /// <summary>
    /// 전부 디스폰 하고 리스트 비우기
    /// </summary>
    private void ClearActiveSlots()
    {
        foreach(var slot in activeSlots)
        {
            ObjcetPoolManager.Instance.Despawn<AchievementSlot>(enumType.UI,slot);
        }
        activeSlots.Clear();
    }


    private void OnEnable()
    {
        //게임 내 주요 이벤트 구독 예정
        GameEvents.OnEnemyKilled += HandleEnemyKilled;
        GameEvents.OnGoldObtained += HandleGoldObtained;
        GameEvents.OnPlayTime += HandlePlayTime;
        GameEvents.OnStageCleared += HandleStageCleared;
       


    }

    private void OnDisable()
    {
        // 구독했으면 구독해제도 같이 해주기
        GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        GameEvents.OnGoldObtained -= HandleGoldObtained;
        GameEvents.OnPlayTime -= HandlePlayTime;
        GameEvents.OnStageCleared -= HandleStageCleared;
    }

    public void AddProgress(int id, double amount)
    {
        if (!achievementsDictionary.TryGetValue(id, out Achievement ach)) return;
        if (ach.isUnLocked) return;

        ach.currentProgress += amount;

        if(ach.currentProgress>=ach.targetProgress)
        {
            ach.currentProgress = ach.targetProgress;
            UnlockAchievement(ach);
        }

        //진행도가 진짜로 변경 되어 UI에게 알림
        OnAchievementUpdated?.Invoke(id);
    }

   
    private void UnlockAchievement(Achievement ach)
    {
        if(ach.isClaimed ==true||ach.isUnLocked ==true)
        {  return; }

        ach.isUnLocked = true;
        ach.isClaimed = true;

        AchievementClearReward(ach);

    }



    private void InitializeDictionary()
    {
        achievementsDictionary.Clear();
        foreach(var ach  in achievements)
        {
            achievementsDictionary[ach.id] = ach;
        }
    }

    
    private void AchievementClearReward(Achievement ach)
    {
        switch(ach.rewardType)
        {
            case RewardType.Gold:
               
                GoldWallet.Instance.Add(ach.rewardAmount);
                break;

            case RewardType.Diamond: 
                
                break;

            case RewardType.Item: 
                
                break;
        }
    }


    #region 저장과 불러오기를 위한 함수와 내용물
    private string SavePath => Path.Combine()

    #endregion



    #region 이벤트 핸들러들 모음
    /// <summary>
    ///  몬스터, 보스 잡고 카운트 할때 들어갈 함수
    /// </summary>
    private void HandleEnemyKilled()
    {

        AddProgress(10001, 1);
    }

    /// <summary>
    /// 돈을 얻는 종류는 여기에 전부 해당하며 내가 가진 돈이 증가할 때 호출해서 카운트 하는 함수
    /// </summary>
    /// <param name="amount"> 얻은 돈의 액수</param>
    private void HandleGoldObtained(double amount)
    {
        AddProgress(10002, amount);
    }

    /// <summary>
    /// 스테이지 클리어시 카운트 하는 함수
    /// </summary>
    private void HandleStageCleared()
    {
        AddProgress(10003,1);
    }

    /// <summary>
    /// 플레이한 시간을 카운트하는 함수
    /// </summary>
    /// <param name="times"></param>
    private void HandlePlayTime(double times)
    {
        AddProgress(10004,times);
    }

    public UniTask OnSceneLoadCreate(SceneId scene)
    {
        throw new NotImplementedException();
    }

    public void Init(SceneId scene)
    {
        throw new NotImplementedException();
    }

    public void OnSceneDestory(SceneId scene)
    {
        throw new NotImplementedException();
    }

    #endregion

}



/*
 
1. 첫 번째 로그인시(로그인 관련 클리어)
2. 튜토리얼 완료
3. 스테이지 관련 업적
4. 캐릭터 성장관련 업적
5. 적 처치 수 관련 업적
6. 플레이 시간 관련 업적
7. 장비 관련 업적
8. 강화 관련 업적(장비 강화 관련,캐릭터 승급?)
9. 도감 관련 업적(지금까지 만난 몬스터, 얻은 장비, 얻은 동료등등)
10. 플레이어 레벨 업적
11. 재화 관련 업적 (지금까지 얼마를 모았음, 얼마를 사용함)




업적 ID 지정







 */
