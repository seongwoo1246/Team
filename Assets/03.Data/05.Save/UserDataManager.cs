/*

 */
using Cysharp.Threading.Tasks;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UtilDebug = DebugLogger<UserDataManager>;

public class UserDataManager : NonMonoSingleton<UserDataManager>
{
    private DatabaseReference rootRef;
    public UserData CurrentData { get; private set; }
    private const string LastLoginTimestamp = "lastLoginTimestamp";


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
                return (true, CurrentData);
            }

            CurrentData = null;
            return (false, null);
        }
        catch (OperationCanceledException)
        {
            return (false, null);
        }
        catch (Exception ex)
        {
            UtilDebug.LogError($"데이터 로드 실패 {ex.Message}");
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

            var updates = new Dictionary<string, object>()
            {
                { $"users/{uid}", newUserData.ToDictionary() },
                { $"nicknames/{nickname}", uid }
            };

            await rootRef.UpdateChildrenAsync(updates).AsUniTask().AttachExternalCancellation(ct);

            CurrentData = newUserData;
            UtilDebug.Log($"신규 유저 데이터 및 닉네임 등록 완료: {nickname} (UID: {uid})");

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            UtilDebug.LogError($"유저 생성 실패 {ex.Message}");
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
            UtilDebug.LogError("SaveAllDataAsync에서 CurrentData가 null입니다. 저장할 데이터가 없습니다.");
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
            UtilDebug.LogError($"데이터 전체 동기화 실패 {ex.Message}");
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
            UtilDebug.LogError("UpdateLastLoginTimeAsync에서 CurrentData가 null입니다. 저장할 데이터가 없습니다.");
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
            UtilDebug.LogError($"접속 시간 갱신 실패 {ex.Message}");
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
    #endregion

    #region Check
    public async UniTask<bool> IsNicknameDuplicateAsync(string nickname, CancellationToken ct = default)
    {
        try
        {
            var snapshot = await rootRef.Child("nickname").Child(nickname).GetValueAsync().AsUniTask().AttachExternalCancellation(ct);
            return snapshot != null && snapshot.Exists;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            UtilDebug.LogError($"닉네임 중복 {ex.Message}");
            return true; ;
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
            string nickname = CurrentData?.nickname;

            var updates = new Dictionary<string, object>
            {
                { $"users/{uid}", null }
            };

            if (!string.IsNullOrEmpty(nickname))
            {
                updates.Add($"nicknames/{nickname}", null);
            }
            await rootRef.UpdateChildrenAsync(updates).AsUniTask().AttachExternalCancellation(ct);

            ClearLocalData();
            UtilDebug.Log($"유저 데이터 및 닉네임 인덱스 삭제 완료 (UID: {uid})");
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            UtilDebug.LogError($"DB 유저 데이터 삭제 실패 {ex.Message}");
            return false;
        }
    }
    #endregion

    public void ClearLocalData() => CurrentData = null;
}