
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = DebugLogger<AchievementManager>;

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

    // 파이어 베이스에 josn으로 저장하기 위한 생성자 및 변환 메서드
    public Achievement() { }

    public Achievement(int id, string title, double targetProgress, double currentProgress, bool isUnLocked)
    {
        this.id = id;
        this.title = title;
        this.targetProgress = targetProgress;
        this.currentProgress = 0;
        this.isUnLocked = false;
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
    // 04시 혹은 일정 시간 지나고 로그인 할 때 마다 한번 씩 할 이벤트
    public static event Action OnLoginDays;
    // 로그인 하고 로그 아웃한 시간을 구해서 얼마나 플레이하는검사할 때 할 이벤트
    public static event Action<double> OnPlayTime;
    // 장비 레벨업 할 때 할 이벤트
    public static event Action OnSumGearLevel;
    // 픽셀들 레벨업 할 때 할 이벤트
    public static event Action OnSumCharLevel;
    // 플레이어 레벨이 올라갈 때 할 이벤트
    public static event Action OnPlayerLevel;
    // 도감을 열 때 할 이벤트
    public static event Action OnUnlockEncyclopedia;
    // 업적을 달성 할 때 할 이벤트
    public static event Action OnUnlockAchievement;


    public static void TriggerOnEnemyKilled() => OnEnemyKilled?.Invoke();
    public static void TriggerOnGoldObtained(double amount) => OnGoldObtained?.Invoke(amount);
    public static void TriggerOnStageCleared() => OnStageCleared?.Invoke();
    public static void TriggerOnLoginDays() => OnLoginDays?.Invoke();
    public static void TriggerOnPlayTime(double times) => OnPlayTime?.Invoke(times);
    public static void TriggerOnSumGearLevel() => OnSumGearLevel?.Invoke();
    public static void TriggerOnSumCharLevel() => OnSumCharLevel?.Invoke();
    public static void TriggerOnPlayerLevel() => OnPlayerLevel?.Invoke();
    public static void TriggerOnUnlockEncyclopedia() => OnUnlockEncyclopedia?.Invoke();
    public static void TriggerOnUnlockAchievement() => OnUnlockAchievement?.Invoke();

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
    private string userId = ""; // 실제 서비스 시 Auth에서 가져오는 UID


    protected override void Awake()
    {
        base.Awake();
        InitializeDictionary();

        //파이어 베이스 루트 참조 초기화 (리얼타임 데이터베이스 기준)
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;

        LoadAchievementsFromFirebase();
    }


    private void OnEnable()
    {
        //게임 내 주요 이벤트 구독 예정
        GameEvents.OnEnemyKilled += HandleEnemyKilled;
        GameEvents.OnGoldObtained += HandleGoldObtained;
        GameEvents.OnLoginDays += HandleOnLoginDays;
        GameEvents.OnPlayerLevel += HandlePlayerLevel;
        GameEvents.OnPlayTime += HandlePlayTime;
        GameEvents.OnStageCleared += HandleStageCleared;
        GameEvents.OnSumCharLevel += HandleSumCharLevel;
        GameEvents.OnSumGearLevel += HandleOnSumGearLevel;
        GameEvents.OnUnlockAchievement += HandleUnlockAchievement;
        GameEvents.OnUnlockEncyclopedia += HandleUnlockEncyclopedia;


    }

    private void OnDisable()
    {
        // 구독했으면 구독해제도 같이 해주기
        GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        GameEvents.OnGoldObtained -= HandleGoldObtained;
        GameEvents.OnLoginDays -= HandleOnLoginDays;
        GameEvents.OnPlayerLevel -= HandlePlayerLevel;
        GameEvents.OnPlayTime -= HandlePlayTime;
        GameEvents.OnStageCleared -= HandleStageCleared;
        GameEvents.OnSumCharLevel -= HandleSumCharLevel;
        GameEvents.OnSumGearLevel -= HandleOnSumGearLevel;
        GameEvents.OnUnlockAchievement -= HandleUnlockAchievement;
        GameEvents.OnUnlockEncyclopedia -= HandleUnlockEncyclopedia;
    }

    public void AddProgress(int id, int amount)
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
        ach.isUnLocked = true;
        //업적 달성 했다고 전달 (만약 API등을 쓰고 있다면 여기서 달성여부를 서버로 보내는 작업을 하고
        //서버에서는 업적에 맞는 보상을 찾아서 지급해주는 코드 넣어주기
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

        // users/{userId}/achievements/{achievementId} 경로에 저장
        await databaseReference.Child("users")
            .Child(userId)
            .Child("achievements")
            .Child(ach.id.ToString())
            .SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {

                if (task.IsFaulted)
                {
                    Debug.LogError($"파이어 베이스 저장 실패 : {task.Exception}");
                }
            });

    }

    /// <summary>
    /// 로그인시 파이어베이스에서 기존 업적 정보 불러오기
    /// </summary>
    private void LoadAchievementsFromFirebase()
    {
        databaseReference.Child("users")
            .Child(userId)
            .Child("achievements").GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"파이어 베이스 데이터 로드 실패");
                    return;
                }

                DataSnapshot snapshot = task.Result;
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
                        Debug.Log($"파이어 베이스 업적 데이터 불러오기 성공");
                    }
                }
            });

    }

    #endregion


    #region 이벤트 핸들러들 모음
    /// <summary>
    ///  몬스터, 보스 잡고 카운트 할때 들어갈 함수
    /// </summary>
    private void HandleEnemyKilled()
    {
        //AddProgress()
    }

    /// <summary>
    /// 돈을 얻는 종류는 여기에 전부 해당하며 내가 가진 돈이 증가할 때 호출해서 카운트 하는 함수
    /// </summary>
    /// <param name="amount"> 얻은 돈의 액수</param>
    private void HandleGoldObtained(double amount)
    {
        //AddProgress()
    }

    /// <summary>
    /// 스테이지 클리어시 카운트 하는 함수
    /// </summary>
    private void HandleStageCleared()
    {
        //AddProgress()
    }


    /// <summary>
    /// 로그인 할 때 카운트 하는 함수
    /// </summary>
    private void HandleOnLoginDays()
    {
        //AddProgress()
    }


    /// <summary>
    /// 플레이한 시간을 카운트하는 함수
    /// </summary>
    /// <param name="times"></param>
    private void HandlePlayTime(double times)
    {
        //AddProgress()
    }


    /// <summary>
    /// 장비 레벨업 할때 카운트 할 함수
    /// </summary>
    private void HandleOnSumGearLevel()
    {
        //AddProgress()
    }

    /// <summary>
    /// 캐릭터 레벨업 할 때 카운트 할 함수
    /// </summary>
    private void HandleSumCharLevel()
    {
        //AddProgress()
    }

    /// <summary>
    /// 플레이어 레벨업시 카운트 함수
    /// </summary>
    private void HandlePlayerLevel()
    {
        //AddProgress()
    }

    /// <summary>
    /// 도감이 열릴 때 마다 카운트 할 함수
    /// </summary>
    private void HandleUnlockEncyclopedia()
    {
        //AddProgress()
    }

    /// <summary>
    /// 업적을 달성 할 때 할 카운트 할 함수
    /// </summary>
    private void HandleUnlockAchievement()
    {
        //AddProgress()
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
