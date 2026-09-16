
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Debug = DebugLogger<AchievementManager>;
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

    // 파이어 베이스에 josn으로 저장하기 위한 생성자 및 변환 메서드
    public Achievement() { }

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
public class AchievementManager : Singleton<AchievementManager>
{
    // 업적을 담아두는 리스트와 딕셔너리
    [SerializeField] private List<Achievement> achievements;
    private Dictionary<int, Achievement> achievementsDictionary = new Dictionary<int, Achievement>();

    

    private DatabaseReference databaseReference; //파이어베이스 DB참조
    private string userId = "";  // 실제 서비스 시 Auth에서 가져오는 UID
    UserInfo userInfo;

    protected override void Awake()
    {
        base.Awake();
        InitializeDictionary();

        //파이어 베이스 루트 참조 초기화 (리얼타임 데이터베이스 기준)
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
       
        userInfo= GetComponent<UserInfo>();
        SetUserId(userInfo);
    }


    public async void SetUserId(UserInfo user)
    {
        userId = user.UID;


        await LoadAchievementsFromFirebase();
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
  
    }

   
    private void UnlockAchievement(Achievement ach)
    {
        if(ach.isClaimed ==true||ach.isUnLocked ==true)
        {  return; }

        ach.isUnLocked = true;
        ach.isClaimed = true;

        // 보상 지급 해주는 코드 넣어주기 우편으로 지급 예정;


        // 클리어 서버에 저장
        SaveAchivementToFirebase(ach);
       
    }



    private void InitializeDictionary()
    {
        achievementsDictionary.Clear();
        foreach(var ach  in achievements)
        {
            achievementsDictionary[ach.id] = ach;
        }
    }

    #region 파이어베이스 관련 함수들
    /// <summary>
    /// 특정 업적에 대한 상태를 파이어 베이스에 저장(Realtime Database)
    /// </summary>
    /// <param name="ach"></param>
    private async void SaveAchivementToFirebase(Achievement ach)
    {
        string json = JsonUtility.ToJson(ach);
        try 
        {
            // users/{userId}/achievements/{achievementId} 경로에 저장
            await databaseReference.Child("users")
              .Child(userId)
              .Child("achievements")
              .Child(ach.id.ToString())
              .SetRawJsonValueAsync(json);
        }
        catch(Exception e) 
        {
            Debug.LogError($"파이어 베이스 저장 실패 : {e.Message}");
        }
 
    }

    /// <summary>
    /// 로그인시 파이어베이스에서 기존 업적 정보 불러오기
    /// </summary>
    private async Task LoadAchievementsFromFirebase()
    {
        try
        {
            DataSnapshot snapshot = await databaseReference
                .Child("users")
                .Child(userId)
                .Child("achievements")
                .GetValueAsync();

            if (snapshot.Exists)
            {
                foreach (DataSnapshot child in snapshot.Children)
                {
                    string json = child.GetRawJsonValue();
                    Achievement loadedAch = JsonUtility.FromJson<Achievement>(json);

                    //불러온 정보를 로컬데이터로 딕셔너리 정보 갱신
                    if (achievementsDictionary.ContainsKey(loadedAch.id))
                    {
                        achievementsDictionary[loadedAch.id].currentProgress = loadedAch.currentProgress;
                        achievementsDictionary[loadedAch.id].isUnLocked = loadedAch.isUnLocked;
                    }

                }
                Debug.Log($"파이어 베이스 업적 데이터 불러오기 성공");
            }
        }
        catch( Exception e ) 
        {
            Debug.LogError($"파이어 베이스 로드 실패 : {e.Message}");
        }

    }

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
