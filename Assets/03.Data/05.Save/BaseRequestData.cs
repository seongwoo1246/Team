/* 담담자 - 송태훈
 
 */

using Cysharp.Threading.Tasks;
using Firebase.Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using static StringConsts.UserConstants;
using UtilDebug = DebugLogger;


/// <summary>
/// 서버 RTDB와 통신하는 모든 유저 데이터의 최상위 추상 베이스 클래스
/// 각 도메인(유저 기본 정보, 캐릭터, 인벤토리 등)에 맞게 대상 노드 경로를 정의하고 Get / Set 로직을 override
/// </summary>
[Serializable]
abstract public class BaseRequestData
{
    public string uid;  // 각 유저의 고유 ID

    // RTDB 최상위 노드 : "users", "charaters", "inventories" 
    protected abstract string RootDomain { get; }

    public BaseRequestData(string uid) => this.uid = uid;

    // 대상 경로 레퍼런스 = root/{RootDomain}/{uid}
    protected DatabaseReference GetTargetRef() => FirebaseDatabase.DefaultInstance.RootReference.Child(RootDomain).Child(uid);

    public abstract UniTask<bool> ExcuteGetAsync(CancellationToken ct = default);
    public abstract UniTask<bool> ExcuteSetAsync(CancellationToken ct = default);

    #region 공통 로깅 및 실행 래퍼(Wrapper)
    protected async UniTask<bool> ExecuteLogOperationAsync(Func<UniTask> action, Func<string> detailInfoGetter = null, [System.Runtime.CompilerServices.CallerMemberName] string callerMethod = "")
    {
        return await ExecuteLogOperationCoreAsync(async () =>
        {
            await action.Invoke();
            return true;
        }, detailInfoGetter, callerMethod);
    }

    protected async UniTask<bool> ExecuteLogOperationCoreAsync(Func<UniTask<bool>> action, Func<string> detailInfoGetter = null, [System.Runtime.CompilerServices.CallerMemberName] string callerMethod = "")
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        string tag = GetType().Name;
        string prefix = $"[{RootDomain}] {callerMethod}";
        string detail = detailInfoGetter != null ? detailInfoGetter?.Invoke() : string.Empty;
        string startMsg = string.IsNullOrEmpty(detail) ? $"{prefix} 요청 시작" : $"{prefix} 요청 시작 -> {detail}";

        UtilDebug.LogWithTag(tag, startMsg);

        try
        {
            bool isSuccess = await action.Invoke();
            if (isSuccess)
            {
                UtilDebug.LogWithTag(tag, $"{prefix} 완료 (성공)");
            }
            else
            {
                UtilDebug.LogWarningWithTag(tag, $"{prefix} 완료 (데이터 없음)");
            }
            return isSuccess;
        }
        catch (Exception ex)
        {
            UtilDebug.LogErrorWithTag(tag, $"{prefix} 실패 {ex.Message}");
            return false;
        }
#else
        try
        {
            return await action.Invoke();
        }
        catch(Exception)
        {
            return false;
        }
#endif  
    }
    #endregion
}
#region 유저 DB
/// <summary>
/// 유저의 기본 프로필 데이터를 관리하는 도메인 클래스
/// RTDB 경로 : root/users/{uid}
/// </summary>
[Serializable]
public class UserProfileRequest : BaseRequestData
{
    protected override string RootDomain => Users;

    public string nickname;
    public int accountLevel;
    public float currentExp;
    public int currentStage;
    public long lastLoginTimestamp;
    public long gold;
    public long dia;

    // UpgradeSystem 연동 데이터
    public Dictionary<string, int> upgradeTrackLevels = new();

    public UserProfileRequest(string uid, string nickname = "") : base(uid)
    {
        this.nickname = nickname;
        this.accountLevel = 1;
        this.currentStage = 1;
        this.lastLoginTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        this.gold = 0;
        this.dia = 0;

        foreach (UpgradeTrack track in Enum.GetValues(typeof(UpgradeTrack)))
        {
            upgradeTrackLevels[track.ToString()] = 0;
        }
    }

    #region 유저 프롭필 전체 동기화 API
    /// <summary>
    /// 서버에서 UserProfile 로드 API
    /// </summary>
    public override async UniTask<bool> ExcuteGetAsync(CancellationToken ct = default)
    {
        return await ExecuteLogOperationCoreAsync(async () =>
        {
            DataSnapshot snapshot = await GetTargetRef().GetValueAsync().AsUniTask().AttachExternalCancellation(ct);
            if (snapshot.Exists && snapshot.Value != null)
            {
                string json = snapshot.GetRawJsonValue();
                JsonConvert.PopulateObject(json, this);
                return true;
            }
            return false;
        }, () => $"Uid: {uid} 로드"
        );
    }
    /// <summary>
    /// 서버에 UserProfile 갱신  API
    /// </summary>

    public override async UniTask<bool> ExcuteSetAsync(CancellationToken ct = default)
    {
        return await ExecuteLogOperationCoreAsync(async () =>
        {
            string json = UnityEngine.JsonUtility.ToJson(this);
            await GetTargetRef().SetRawJsonValueAsync(json).AsUniTask().AttachExternalCancellation(ct);
            return true;
        }, () => $"Nick: {nickname}, Lv: {accountLevel}, Stage: {currentStage} 저장"
        );
    }
    #endregion

    #region 유저 프로필 단일 동기화 API
    /// <summary>
    /// 서버에 UserProfile 중 단일 갱신 API
    /// </summary>
    /// <param name="fieldName">변경할 API(필드)</param>
    /// <param name="value">변경할 값</param>
    public async UniTask<bool> UpdateSingleFieldAsync(string fieldName, object value, CancellationToken ct = default)
    {
        return await ExecuteLogOperationCoreAsync(async () =>
        {
            await GetTargetRef().Child(fieldName).SetValueAsync(value).AsUniTask().AttachExternalCancellation(ct);
            return true;
        }, () => $"Field: {fieldName}, Value: {value} 갱신"
        );
    }

    /// <summary>
    /// 파티 강화 시 해당 트랙 레벨 단일 노드 갱신 API
    /// </summary>
    /// <param name="track">트랙 노드</param>
    /// <param name="newLevel">갱신 레벨</param>
    public async UniTask<bool> UpdateUpgradeTrackAsync(UpgradeTrack track, int newLevel, CancellationToken ct = default)
    {
        string trackKey = track.ToString();
        upgradeTrackLevels[trackKey] = newLevel;

        return await ExecuteLogOperationCoreAsync(async () =>
        {
            await GetTargetRef().Child(UpgradeTrackLevels).Child(trackKey).SetValueAsync(newLevel).AsUniTask().AttachExternalCancellation(ct);
            return true;
        }, () => $"Track: {trackKey}, Lv: {newLevel} 갱신");
    }
    #endregion
}
#endregion

#region 보유 중인 캐릭터 DB
/// <summary>
/// 캐릭터 DTO / DB에 저장되는 단일 캐릭터 정보
/// </summary>
[Serializable]
public class CharacterSaveData
{
    public string characterId;
    public bool isUnlocked = true;
    public int partySlot = -1;  // 파티 배치 슬롯 ( -1 : 미편성 )

    // 6개의 슬롯별 장착 중인 장비의 instanceID 매핑
    public Dictionary<string, string> equippedItems = new();
}

/// <summary>
/// 보유 캐릭터 및 슬롯별 장착 정보를 관리하는 도메인 클래스
/// RTDB 경로 : root/characters/{uid}
/// </summary>
[Serializable]
public class CharacterRequest : BaseRequestData
{
    protected override string RootDomain => Characters;

    // Key : characterId ( char_warrior / char_mage / char_healer )
    public Dictionary<string, CharacterSaveData> characterDictionary = new();
    public CharacterRequest(string uid) : base(uid) { }

    #region 캐릭터 전체 동기화 API
    public override async UniTask<bool> ExcuteGetAsync(CancellationToken ct = default)
    {
        return await ExecuteLogOperationCoreAsync(
            async () =>
            {
                DataSnapshot snapshot = await GetTargetRef().GetValueAsync().AsUniTask().AttachExternalCancellation(ct);
                if (snapshot.Exists && snapshot.Value != null)
                {
                    string json = snapshot.GetRawJsonValue();
                    characterDictionary = JsonConvert.DeserializeObject<Dictionary<string, CharacterSaveData>>(json) ?? new();
                    return true;
                }
                return false;
            }, () => $"Uid: {uid} 로드"
            );
    }

    public override async UniTask<bool> ExcuteSetAsync(CancellationToken ct = default)
    {
        return await ExecuteLogOperationCoreAsync(async () =>
        {
            string json = JsonConvert.SerializeObject(characterDictionary);
            await GetTargetRef().SetRawJsonValueAsync(json).AsUniTask().AttachExternalCancellation(ct);
            return true;
        }, () => $"CharCount: {characterDictionary.Count} 저장"
        );
    }
    #endregion

    #region 캐릭터 개별 갱신 API
    /// <summary>
    /// 단일 캐릭터의 장비 장착 API
    /// root/characters/{uid}/{charId}/equippedSlotMap/{slot}
    /// </summary>
    public async UniTask<bool> SetEquippedSlotAsync(string charId, EquipmentSlot slot, string instanceId, CancellationToken ct = default)
    {
        if (!characterDictionary.TryGetValue(charId, out var charData)) return false;

        string slotkey = slot.ToString();

        return await ExecuteLogOperationCoreAsync(async () =>
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                charData.equippedItems.Remove(slotkey);
                await GetTargetRef().Child(charId).Child(EquippedSlotMap).Child(slotkey).RemoveValueAsync().AsUniTask().AttachExternalCancellation(ct);
            }
            else
            {
                charData.equippedItems[slotkey] = instanceId;
                await GetTargetRef().Child(charId).Child(EquippedSlotMap).Child(slotkey).SetValueAsync(instanceId).AsUniTask().AttachExternalCancellation(ct);
            }

            return true;
        }, () => $"Char: {charId}, Slot: {slot}, InstId: {instanceId} 변경"
        );
    }
    /// <summary>
    /// 단일 캐릭터의 장비 해제 API
    /// </summary>
    /// <param name="charId"></param>
    /// <param name="slot"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async UniTask<bool> UnequipSlotAsync(string charId, EquipmentSlot slot, CancellationToken ct = default)
    {
        if(!characterDictionary.TryGetValue(charId,out var charData)) return false;

        string slotkey = slot.ToString();

        // 이미 비어 있으면 서버 요청 x
        if (!charData.equippedItems.ContainsKey(slotkey)) return true;

        return await ExecuteLogOperationCoreAsync(async () =>
        {
            charData.equippedItems.Remove(slotkey);

            await GetTargetRef().Child(charId).Child(EquippedSlotMap).Child(slotkey).RemoveValueAsync().AsUniTask().AttachExternalCancellation(ct);
            return true;
        }, () => $"Char: {charId}, Slot: {slot} 해제"
        );
    }
    /// <summary>
    /// Account level에 따른 캐릭터 해금 API
    /// 또는 새로운 게임 캐릭터 제작 시 사용 가능
    /// </summary>
    /// <param name="newChar">새로운 캐릭터 DB</param>
    public async UniTask<bool> AddOrUpdateCharacterAsync(CharacterSaveData newChar, CancellationToken ct = default)
    {
        if (newChar == null || string.IsNullOrEmpty(newChar.characterId))
            return false;

        return await ExecuteLogOperationCoreAsync(async () =>
        {
            characterDictionary[newChar.characterId] = newChar;

            string charJson = JsonConvert.SerializeObject(newChar);
            await GetTargetRef().Child(newChar.characterId).SetRawJsonValueAsync(charJson).AsUniTask().AttachExternalCancellation(ct);
            return true;
        }
        , () => $"Char: {newChar.characterId}, PartySlot: {newChar.partySlot} 해금"
        );
    }

    /// <summary>
    /// 파티 배치 변경 시 partSlot 필드만 단톡 갱신 API
    /// </summary>
    /// <param name="charId">캐릭터 고유 ID</param>
    /// <param name="slotIndex">슬롯 Index</param>
    public async UniTask<bool> UpdatePartySlotAsync(string charId, int slotIndex, CancellationToken ct = default)
    {
        if (!characterDictionary.TryGetValue(charId, out var charData))
            return false;

        return await ExecuteLogOperationCoreAsync(async () =>
        {
            charData.partySlot = slotIndex;
            await GetTargetRef().Child(charId).Child(PartySlot).SetValueAsync(slotIndex).AsUniTask().AttachExternalCancellation(ct);
            return true;
        }, () => $"Char: {charId}, Slot: {slotIndex} 갱신"
        );
    }
    #endregion

    // 파티 슬롯 동시 변경을 할 예정이면 여기에 추가
}
#endregion

#region 보유 중인 인벤토리 DB
[Serializable]
public class EquipmentSaveDTO
{
    public string dataId;           // 장비 SO 고유 ID
    public float rollPercent;       // 드랍 시 제공되는 기본 Roll %
    public int enhanceLevel;        // 장비 강화 단계
    public float totalEnhanceBonus; // 강화 누적 보너스 합계 %
}

/// <summary>
/// 인벤토리 전체를 묶는 DTO
/// </summary>
[Serializable]
public class InventorySaveData
{
    // Key : 고유 식별자 instanceID (GUID)
    public Dictionary<string, EquipmentSaveDTO> equipments = new();

    // Key : SO 내 고유 ID / Vaule : 수량
    public Dictionary<string, int> consumables = new();
}

[Serializable]
public class InventoryRequest : BaseRequestData
{
    protected override string RootDomain => Inventories;

    public InventorySaveData Data { get; private set; } = new();
    public InventoryRequest(string uid) : base(uid) { }

    #region 인벤토리 전체 동기화 API
    public override async UniTask<bool> ExcuteGetAsync(CancellationToken ct = default)
    {
        return await ExecuteLogOperationCoreAsync(async () =>
        {
            DataSnapshot snapshot = await GetTargetRef().GetValueAsync().AsUniTask().AttachExternalCancellation(ct);
            if (snapshot.Exists && snapshot.Value != null)
            {
                string json = snapshot.GetRawJsonValue();
                Data = JsonConvert.DeserializeObject<InventorySaveData>(json) ?? new();
                return true;
            }
            return false;
        }, () => $"Uid: {uid} 로드"
        );
    }

    public override async UniTask<bool> ExcuteSetAsync(CancellationToken ct = default)
    {
        return await ExecuteLogOperationCoreAsync(async () =>
        {
            string json = JsonConvert.SerializeObject(Data);
            await GetTargetRef().SetRawJsonValueAsync(json).AsUniTask().AttachExternalCancellation(ct);
            return true;
        }, () => $"EquipCount: {Data.equipments.Count}, ConsumableCount: {Data.consumables.Count} 저장"
        );
    }
    #endregion

    #region 장비 개별 갱신 API

    /// <summary>
    /// 장비 획득 시 추가 API / Key : instanceID
    /// root/inventories/{uid}/equipments/{instanceId}
    /// </summary>
    /// <param name="instanceId"> 장비 식별 ID ( GUID )</param>
    /// <param name="newEquip"> 추가되는 새로운 장비 </param>
    public async UniTask<bool> AddEquipmentAsync(string instanceId, EquipmentSaveDTO newEquip, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(instanceId) || newEquip == null)
            return false;

        return await ExecuteLogOperationCoreAsync(async () =>
        {
            Data.equipments[instanceId] = newEquip;
            string json = JsonConvert.SerializeObject(newEquip);
            await GetTargetRef().Child(Equipments).Child(instanceId).SetRawJsonValueAsync(json).AsUniTask().AttachExternalCancellation(ct);
            return true;
        }, () => $"InstId: {instanceId}, DataId: {newEquip.dataId} 추가");
    }

    /// <summary>
    /// 장비 버리기 API ( 단일 노드 삭제 ) - 판매에 넣는 것도 가능.
    /// </summary>
    /// <param name="instanceId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async UniTask<bool> RemoveEquipmentAsync(string instanceId, CancellationToken ct = default)
    {
        if (!Data.equipments.ContainsKey(instanceId))
            return false;

        return await ExecuteLogOperationCoreAsync(async () =>
        {
            Data.equipments.Remove(instanceId);
            await GetTargetRef().Child(Equipments).Child(instanceId).RemoveValueAsync().AsUniTask().AttachExternalCancellation(ct);
            return true;
        }, () => $"InstId: {instanceId} 삭제");
    }

    /// <summary>
    /// 장비 강화 성공 시 강화 단계 및 누적 보너스만 부분 동기화 API
    /// </summary>
    /// <param name="instanceId"> 강화한 장비 식별 ID ( GUID )</param>
    /// <param name="newLevel"> 갱신된 강화 단계 </param>
    /// <param name="newBonus"> 갱신된 누적 보너스 </param>
    public async UniTask<bool> UpdateEquipmentEnhanceAsync(string instanceId, int newLevel, float newBonus, CancellationToken ct = default)
    {
        if (!Data.equipments.TryGetValue(instanceId, out var item))
            return false;
        item.enhanceLevel = newLevel;
        item.totalEnhanceBonus = newBonus;

        var updates = new Dictionary<string, object>
        {
            { $"equipments/{instanceId}/enhanceLevel", newLevel },
            { $"equipments/{instanceId}/enhanceBonus", newBonus }
        };

        return await ExecuteLogOperationCoreAsync(async () =>
        {
            await GetTargetRef().UpdateChildrenAsync(updates).AsUniTask().AttachExternalCancellation(ct);
            return true;
        }, () => $"InstId: {instanceId}, Lv: +{newLevel}, Bonus: {newBonus:F1}% 강화 갱신");
    }
    #endregion

    #region 소모품 / 재료 개별 조작 API
    /// <summary>
    /// 소모품 / 재료 수량 변경 ( 사용 / 획득 ) API
    /// root/inventories/{uid}/consumables/{itemId}
    /// </summary>
    public async UniTask<bool> UpdateConsumableCountAsync(string itemId, int newCount, CancellationToken ct = default)
    {
        return await ExecuteLogOperationCoreAsync(async () =>
        {
            if (newCount <= 0)
            {
                Data.consumables.Remove(itemId);
                await GetTargetRef().Child(Consumables).Child(itemId).RemoveValueAsync().AsUniTask().AttachExternalCancellation(ct);
            }
            else
            {
                Data.consumables[itemId] = newCount;
                await GetTargetRef().Child(Consumables).Child(itemId).SetValueAsync(newCount).AsUniTask().AttachExternalCancellation(ct);
            }
            return true;
        }, () => $"ItemId: {itemId}, Count: {newCount}");

        #endregion
    }
}
#endregion