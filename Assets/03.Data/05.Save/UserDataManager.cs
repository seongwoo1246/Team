/*

 */
using Cysharp.Threading.Tasks;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading;
using Debug = DebugLogger<UserDataManager>;

public class UserDataManager : NonMonoSingleton<UserDataManager>
{
    private DatabaseReference rootRef;
    public UserData CurrentData { get; private set; }
    private const string LastLoginTimestamp = "lastLoginTimestamp";
    private const string IsTutorialCompleted = "isTutorialCompleted";


    public override void Init()
    {
        base.Init();
        rootRef = FirebaseDatabase.DefaultInstance.RootReference;
    }

    private DatabaseReference GetUserRef(string uid) => rootRef.Child("users").Child(uid);

    #region [Read & Load]
    /// <summary>
    /// RTDB에서 유저 데이터를 비동기적으로 로드합니다.
    /// </summary>
    public async UniTask<(bool exists, UserData data)> LoadUserDataAsync(string uid, CancellationToken ct = default)
    {
        try
        {
            DataSnapshot snapshot = await GetUserRef(uid).GetValueAsync().AsUniTask().AttachExternalCancellation(ct);
            if (snapshot.Exists && snapshot.Value != null)
            {
                string json = snapshot.GetRawJsonValue();
                UserData data = UnityEngine.JsonUtility.FromJson<UserData>(json);
                CurrentData = data;
                return (true, data);
            }

            return (false, null);
        }
        catch (OperationCanceledException)
        {
            return (false, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"데이터 로드 실패 {ex.Message}");
            return (false, null);
        }
    }
    #endregion

    #region [Create]
    /// <summary>
    /// 첫 로그인 시 신규 유저 데이터를 생성하고 RTDB에 저장합니다.
    /// </summary>
    public async UniTask<bool> CreateUserDataAsync(string uid, string nickname, CancellationToken ct = default)
    {
        try
        {
            UserData newUserData = UserData.CreateNewUser(uid, nickname);
            string json = UnityEngine.JsonUtility.ToJson(newUserData);

            await GetUserRef(uid).SetRawJsonValueAsync(json).AsUniTask().AttachExternalCancellation(ct);
            CurrentData = newUserData;
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"유저 생성 실패 {ex.Message}");
            return false;
        }
    }
    #endregion

    #region [Sync / Update]
    /// <summary>
    /// 메모리에 올라온 CurrentData의 전체 데이터를 서버(RTOB)와 동기화(덮어쓰기)
    /// </summary>
    public async UniTask<bool> SaveAllDataAsync(CancellationToken ct = default)
    {
        if (CurrentData == null)
        {
            Debug.LogError("SaveAllDataAsync에서 CurrentData가 null입니다. 저장할 데이터가 없습니다.");
            return false;
        }
        try
        {
            string json = UnityEngine.JsonUtility.ToJson(CurrentData);
            await GetUserRef(CurrentData.uid).SetRawJsonValueAsync(json).AsUniTask().AttachExternalCancellation(ct);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"데이터 전체 동기화 실패 {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 방치 보상 계산을 위한 마지막 접속 시간 단일 필드 동기화
    /// </summary>
    public async UniTask UpdateLastLoginTimeAsync(CancellationToken ct = default)
    {
        if (CurrentData == null)
        {
            Debug.LogError("UpdateLastLoginTimeAsync에서 CurrentData가 null입니다. 저장할 데이터가 없습니다.");
            return;
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        CurrentData.lastLoginTimestamp = now;

        try
        {
            await GetUserRef(CurrentData.uid).Child(LastLoginTimestamp).SetValueAsync(now).AsUniTask().AttachExternalCancellation(ct);
        }
        catch (Exception ex)
        {
            Debug.LogError($"접속 시간 갱신 실패 {ex.Message}");
        }
    }

    // 특정 필요한 필드만 선택적으로 부분 갱신 메서드 추가 가능
    /*
         public async UniTask UpdateCurrentAsync(long gold, CancellationToken ct =default)
    {
        if (CurrentData == null)
        {
            Debug.LogError("UpdateCurrentAsync에서 CurrentData가 null입니다. 저장할 데이터가 없습니다.");
            return;
        }
        //CurrentData.gold  // 동기화해야하는 부분 데이터 추가
        var updates = new Dictionary<string, object>
        {
            {"gold", gold },
        };

        await GetUserRef(CurrentData.uid).UpdateChildrenAsync(update).AsUniTask().AttachExternalCancellation(ct);
    }

     */

    /// <summary>
    /// 튜토리얼 완료 상태 동기화
    /// </summary>
    public async UniTask SetTutorialCompletedAsync(bool isCompleted, CancellationToken ct = default)
    {
        if (CurrentData == null)
        {
            Debug.LogError("UpdateCurrentAsync에서 CurrentData가 null입니다. 저장할 데이터가 없습니다.");
            return;
        }

        CurrentData.isTutorialCompleted = isCompleted;
        try
        {
            await GetUserRef(CurrentData.uid).Child(IsTutorialCompleted).SetValueAsync(isCompleted).AsUniTask().AttachExternalCancellation(ct);
        }
        catch (Exception ex)
        {
            Debug.LogError($"튜토리얼 완료 갱신 실패 {ex.Message}");
        }
    }
    #endregion

    #region [Delete]
    /// <summary>
    /// 서버 RTDB 상의 유저 데이터를 영구 삭제
    /// </summary>
    public async UniTask<bool> DeleteUserDataAsync(string uid, CancellationToken ct = default)
    {
        try
        {
            await GetUserRef(uid).RemoveValueAsync().AsUniTask().AttachExternalCancellation(ct);
            ClearLocalData();
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"DB 유저 데이터 삭제 실패 {ex.Message}");
            return false;
        }
    }
    #endregion

    private void ClearLocalData() => CurrentData = null;
}