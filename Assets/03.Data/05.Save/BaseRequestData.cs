using Cysharp.Threading.Tasks;
using Firebase.Database;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Threading;
//using UnityEngine;

public enum DataSyncAction
{
    Get, Set
}

[Serializable]
abstract public class BaseRequestData
{
    public string uid;

    // RTDB 최상위 노드 : "users", "charaters", "inventories" 
    protected abstract string RootDomain { get; }

    public BaseRequestData(string uid) => this.uid = uid;

    // 대상 경로 레퍼런스 = root/{RootDomain}/{uid}
    protected DatabaseReference GetTargetRef() => FirebaseDatabase.DefaultInstance.RootReference.Child(RootDomain).Child(uid);

    public abstract UniTask<bool> GetAsync(CancellationToken ct = default);
    public abstract UniTask<bool> SetAsync(CancellationToken ct = default);

    public UniTask<bool> SyncAsync(DataSyncAction action, CancellationToken ct = default)
    {
        return action switch
        {
            DataSyncAction.Get => GetAsync(ct),
            DataSyncAction.Set => SetAsync(ct),
            _ => UniTask.FromResult(false)
        };
    }
}

[Serializable]
public class UserProgileRequest : BaseRequestData
{
    protected override string RootDomain => "users";

    public string nickname;
    public int accountLevel;
    public int curreStage;
    public long lastLoginTimestamp;
    public long gold;
    public long dia;

    public UserProgileRequest(string uid, string nickname = "") : base(uid)
    {
        this.nickname = nickname;
        this.accountLevel = 1;
        this.curreStage = 1;
        this.lastLoginTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        this.gold = 0;
        this.dia = 0;
    }

    public override async UniTask<bool> GetAsync(CancellationToken ct = default)
    {
        DataSnapshot snapshot = await GetTargetRef().GetValueAsync().AsUniTask().AttachExternalCancellation(ct);
        if (snapshot.Exists && snapshot.Value != null)
        {
            string json = snapshot.GetRawJsonValue();
            UnityEngine.JsonUtility.FromJsonOverwrite(json, this);
            return true;
        }
        return false;
    }

    public override async UniTask<bool> SetAsync(CancellationToken ct = default)
    {
        string json = UnityEngine.JsonUtility.ToJson(this);
        await GetTargetRef().SetRawJsonValueAsync(json).AsUniTask().AttachExternalCancellation(ct);
        return true;
    }

    public async UniTask<bool> UpdateSingleFieldAsync(string fieldName, object value, CancellationToken ct = default)
    {
        await GetTargetRef().Child(fieldName).SetValueAsync(value).AsUniTask().AttachExternalCancellation(ct);
        return true;
    }
}


/// <summary>
/// 캐릭터가 장착 중인 단일 장비 스냅샷 DTO
/// </summary>
[Serializable] 
public class EquippedItemDTO
{
    public string itemId;
    public int enhace;
    public float rollPercent;
}

/// <summary>
/// 
/// </summary>
[Serializable]
public class CharacterSaveData
{
    public string characterId;
    public int level = 1;
    public bool isUnlocked = true;
    public int partySlot = -1;

    public Dictionary<string, EquippedItemDTO> equippedItems = new();
}

//[Serializable]
//public class CharacterRequest : BaseRequestData
//{
//    protected override string RootDomain => "characters";

//    public List<CharacterBase> characterList = new();
//    public CharacterRequest(string uid) : base(uid) { }

//    public override async UniTask<bool> GetAsync(CancellationToken ct = default)
//    {
//        DataSnapshot snapshot = await GetTargetRef().GetValueAsync().AsUniTask().AttachExternalCancellation(ct);
//        if (snapshot.Exists && snapshot.Value != null)
//        {
//            string json = snapshot.GetRawJsonValue();
//            var loaded = UnityEngine.JsonUtility.FromJson<CharacterRequest>(json);
//            return true;
//        }
//        return false;
//    }
//}