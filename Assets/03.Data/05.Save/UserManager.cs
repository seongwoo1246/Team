/* 담담자 - 송태훈

 */
using Cysharp.Threading.Tasks;
using Firebase.Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using static StringConsts.UserConstants;
using UtilDebug = DebugLogger<UserManager>;

public class UserManager : NonMonoSingleton<UserManager>
{
    private DatabaseReference rootRef;
    public UserInfo CurrentUser { get; private set; }

    public override void Init()
    {
        base.Init();
        rootRef = FirebaseDatabase.DefaultInstance.RootReference;
    }

    private DatabaseReference GetUserRef(string uid) => rootRef?.Child(Users).Child(uid);
    private DatabaseReference GetCharacterRef(string uid) => rootRef?.Child(Characters).Child(uid);
    private DatabaseReference GetInventoryRef(string uid) => rootRef?.Child(Inventories).Child(uid);
    private DatabaseReference GetNicknameRef(string nickname) => rootRef?.Child(Nicknames).Child(nickname);

    #region [Read & Load] 전체 로드
    /// <summary>
    /// RTDB에서 유저 데이터(프로필, 캐릭터, 인벤토리 전체 도메인을 비동기 병렬 로드)
    /// </summary>
    public async UniTask<(bool exists, UserInfo data)> LoadUserInfoAsync(string uid, CancellationToken ct = default)
    {
        try
        {
            UserInfo tempUser = new UserInfo(uid, string.Empty);

            var (profileOk, charOk, invOk) = await UniTask.WhenAll(
                tempUser.Profile.ExcuteGetAsync(ct),
                tempUser.Characters.ExcuteGetAsync(ct),
                tempUser.Inventory.ExcuteGetAsync(ct)
            );

            // 유저 프로필이 없으면 신규 유저로 판정
            if (!profileOk)
            {
                CurrentUser = null;
                return (false, null);
            }

            CurrentUser = tempUser;
            UtilDebug.Log($"전체 유저 데이터 로드 성공 (UID: {uid}");
            return (true, CurrentUser);
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

    #region [Create] 회원 가입
    /// <summary>
    /// 회원가입. 닉네임 중복 인덱스와 초기 데이터를 단일 트랜잭션으로 생성
    /// </summary>
    public async UniTask<bool> CreateUserInfoAsync(string uid, string nickname, CancellationToken ct = default)
    {
        try
        {
            UserInfo newUserData = new UserInfo(uid, nickname);
            var warriorData = new CharacterSaveData
            {
                characterId = "char_warrior",
                isUnlocked = true,
                partySlot = 0
            };

            var mageData = new CharacterSaveData
            {
                characterId = "char_mage",
                isUnlocked = true,
                partySlot = 1
            };

            var healerData = new CharacterSaveData
            {
                characterId = "char_healer",
                isUnlocked = true,
                partySlot = 2
            };

            newUserData.Characters.characterDictionary[warriorData.characterId] = warriorData;
            newUserData.Characters.characterDictionary[mageData.characterId] = mageData;
            newUserData.Characters.characterDictionary[healerData.characterId] = healerData;
            newUserData.Inventory.Data.consumables["Material"] = 1;

            // 1. 순수 JSON 문자열 직렬화
            string profileJson = JsonConvert.SerializeObject(newUserData.Profile);
            string charJson = JsonConvert.SerializeObject(newUserData.Characters.characterDictionary);
            string inventoryJson = JsonConvert.SerializeObject(newUserData.Inventory.Data);

            // 2. SetVauleAsync를 병렬로 실행
            // => JsonConvert.DeserializeObject()를 사용했으나 Firebase SDK 내부 파서에서 Newtonsoft.Json의 내부 JObject나 JArray 타입을 이해하지 못해 병렬 호출로 변경
            await UniTask.WhenAll(
                GetNicknameRef(nickname).SetValueAsync(uid).AsUniTask().AttachExternalCancellation(ct),
                GetUserRef(uid).SetRawJsonValueAsync(profileJson).AsUniTask().AttachExternalCancellation(ct),
                GetCharacterRef(uid).SetRawJsonValueAsync(charJson).AsUniTask().AttachExternalCancellation(ct),
                GetInventoryRef(uid).SetRawJsonValueAsync(inventoryJson).AsUniTask().AttachExternalCancellation(ct)
            );

            CurrentUser = newUserData;
            UtilDebug.Log($"신규 유저 생성 및 닉네임 등록 완료: {nickname} (UID: {uid})");
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            UtilDebug.LogError($"신규 유저 생성 실패 {ex.Message}");
            return false;
        }
    }
    #endregion

    #region [Save & Sync]
    /// <summary>
    /// 전체 도메인 상태를 RTDB에 일괄 저장
    /// </summary>
    public async UniTask<bool> SaveAllInfoAsync(CancellationToken ct = default)
    {
        if (CurrentUser == null)
        {
            UtilDebug.LogError("SaveAllInfoAsync : 저장할 유저 데이터가 없습니다.");
            return false;
        }

        var (profileOk, charOk, invOk) = await UniTask.WhenAll(
            CurrentUser.Profile.ExcuteSetAsync(ct),
            CurrentUser.Characters.ExcuteSetAsync(ct),
            CurrentUser.Inventory.ExcuteSetAsync(ct)
        );

        return profileOk && charOk && invOk;
    }

    /// <summary>
    /// 방치 보상 계산을 위한 마지막 접속 시간 단일 필드 동기화
    /// </summary>
    public async UniTask UpdateLastLoginTimeAsync(CancellationToken ct = default)
    {
        if (CurrentUser == null)
        {
            UtilDebug.LogError("UpdateLastLoginTimeAsync : 저장할 유저 데이터가 없습니다.");
            return;
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        CurrentUser.Profile.lastLoginTimestamp = now;
        await CurrentUser.Profile.UpdateSingleFieldAsync(LastLoginTimestamp, now, ct);
    }
    #endregion

    #region [Facade API : 단일 도메인]
    // 골드 단일 갱신
    public async UniTask<bool> UpdateGoldAsync(double newGold, CancellationToken ct =default)
    {
        if (CurrentUser == null) return false;
        CurrentUser.Profile.gold = newGold;
        return await CurrentUser.Profile.UpdateSingleFieldAsync("gold", newGold, ct);
    }
    // 다이아 단일 갱신
    public async UniTask<bool> UpdateDiaAsync(double newDia, CancellationToken ct = default)
    {
        if (CurrentUser == null) return false;
        CurrentUser.Profile.dia = newDia;
        return await CurrentUser.Profile.UpdateSingleFieldAsync("dia", newDia, ct);
    }

    /// <summary>
    /// 캐릭터 장비 장착(스왑)
    /// </summary>
    public async UniTask<bool> EquipItemAsync(string charId, EquipmentSlot slot, string instanceId, CancellationToken ct = default)
    {
        if (CurrentUser == null) return false;
        return await CurrentUser.Characters.SetEquippedSlotAsync(charId, slot, instanceId, ct);
    }

    /// <summary>
    /// 캐릭터 장비 해제
    /// </summary>
    public async UniTask<bool> UnequipItemAsync(string charId, EquipmentSlot slot, CancellationToken ct = default)
    {
        if (CurrentUser == null) return false;
        return await CurrentUser.Characters.UnequipSlotAsync(charId, slot, ct);
    }

    /// <summary>
    /// 장비 강화
    /// </summary>
    public async UniTask<bool> EnhanceEquipmentAsync(string instanceId, int newLevel, float newBonus, CancellationToken ct = default)
    {
        if (CurrentUser == null) return false;
        return await CurrentUser.Inventory.UpdateEquipmentEnhanceAsync(instanceId, newLevel, newBonus, ct);
    }

    /// <summary>
    /// 재료 소모
    /// </summary>
    public async UniTask<bool> UpdateConsumableCountAsync(string itemId, int count, CancellationToken ct = default)
    {
        if (CurrentUser == null) return false;
        return await CurrentUser.Inventory.UpdateConsumableCountAsync(itemId, count, ct);
    }

    /// <summary>
    /// 파티 공동 능력치 강화
    /// </summary>
    public async UniTask<bool> UpgradeTrackLevelAsync(UpgradeTrack track, int newLevel, CancellationToken ct = default)
    {
        if (CurrentUser == null) return false;
        return await CurrentUser.Profile.UpdateUpgradeTrackAsync(track, newLevel, ct);
    }
    #endregion

    #region [Facade API : 복합 도메인 트랜잭션]
    // ex) 장비 판매 트랜잭션 : 착용 해제 + 인벤토리 제거 + 골드 증가
    // 이러한 트랜잭션 가능

    /// <summary>
    /// 파티 슬롯 일괄 갱신 (3자리 슬롯 인덱스 전체 매핑)
    /// </summary>
    public async UniTask<bool> UpdateAllPartySlotAsync(Dictionary<string, int> slotMap, CancellationToken ct = default)
    {
        if(CurrentUser == null) return false;
        var updates = new Dictionary<string, object>();
        foreach(var kvp in slotMap)
        {
            string charId = kvp.Key;
            int slotIndex = kvp.Value;
            if(CurrentUser.Characters.characterDictionary.TryGetValue(charId, out var charData))
            {
                charData.partySlot = slotIndex;
                updates[$"{StringConsts.UserConstants.Characters}/{CurrentUser.UID}/{charId}/partySlot"] = slotIndex;
            }
        }

        try
        {
            await rootRef.UpdateChildrenAsync(updates).AsUniTask().AttachExternalCancellation(ct);
            return true;
        }
        catch (OperationCanceledException)
        {
            // 씬 전환 등으로 정상 취소된 경우 조용히 false 반환
            return false;
        }
        catch (Exception ex)
        {
            UtilDebug.LogError($"파티 슬롯 동기화 실패: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 장비 장착 트랜잭션: 대상 슬롯 장착 및 기존 장착자 해제 원자적 처리
    /// </summary>
    public async UniTask<bool> SwapEquipmentTransactionAsync(string targetCharId, EquipmentSlot slot, string newInstanceId, string previousOwnerCharId = null, CancellationToken ct = default)
    {
        if (CurrentUser == null) return false;

        string slotKey = slot.ToString();
        var updates = new Dictionary<string, object>();
        string uid = CurrentUser.UID;

        // 1. 기존 착용 캐릭터가 있었다면 해당 캐릭터 슬롯에서 제거
        if (!string.IsNullOrEmpty(previousOwnerCharId) && previousOwnerCharId != targetCharId)
        {
            if (CurrentUser.Characters.characterDictionary.TryGetValue(previousOwnerCharId, out var prevChar))
            {
                prevChar.equippedItems.Remove(slotKey);
                updates[$"{StringConsts.UserConstants.Characters}/{uid}/{previousOwnerCharId}/{EquippedSlotMap}/{slotKey}"] = null;
            }
        }

        // 2. 대상 캐릭터 슬롯에 새 instanceId 지정
        if (CurrentUser.Characters.characterDictionary.TryGetValue(targetCharId, out var targetChar))
        {
            targetChar.equippedItems[slotKey] = newInstanceId;
            updates[$"{StringConsts.UserConstants.Characters}/{uid}/{targetCharId}/{EquippedSlotMap}/{slotKey}"] = newInstanceId;
        }

        try
        {
            await rootRef.UpdateChildrenAsync(updates).AsUniTask().AttachExternalCancellation(ct);
            return true;
        }
        catch (Exception ex)
        {
            UtilDebug.LogError($"장비 장착 트랜잭션 실패: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 장비 강화 트랜잭션: 재료 차감 + 장비 강화 수치 동시 갱신
    /// </summary>
    public async UniTask<bool> EnhanceEquipmentTransactionAsync(string instanceId, int newLevel, float newBonus, string materialItemId, int remainingMaterialCount, CancellationToken ct = default)
    {
        if (CurrentUser == null) return false;

        string uid = CurrentUser.UID;
        var updates = new Dictionary<string, object>
    {
        { $"{Inventories}/{uid}/{Equipments}/{instanceId}/enhanceLevel", newLevel },
        { $"{Inventories}/{uid}/{Equipments}/{instanceId}/totalEnhanceBonus", newBonus },
        { $"{Inventories}/{uid}/{Consumables}/{materialItemId}", remainingMaterialCount }
    };

        // 로컬 메모리 상태 갱신
        if (CurrentUser.Inventory.Data.equipments.TryGetValue(instanceId, out var equip))
        {
            equip.enhanceLevel = newLevel;
            equip.totalEnhanceBonus = newBonus;
        }
        CurrentUser.Inventory.Data.consumables[materialItemId] = remainingMaterialCount;

        try
        {
            await rootRef.UpdateChildrenAsync(updates).AsUniTask().AttachExternalCancellation(ct);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            UtilDebug.LogError($"장비 강화 트랜잭션 실패: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region [Check API : 닉네임 중복 검사]
    public async UniTask<bool> IsNicknameDuplicateAsync(string nickname, CancellationToken ct = default)
    {
        try
        {
            var snapshot = await GetNicknameRef(nickname).GetValueAsync().AsUniTask().AttachExternalCancellation(ct);
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


    #region [Delete : 계정 탈퇴(삭제) API]
    /// <summary>
    /// 서버 RTDB 상의 유저 데이터를 영구 삭제
    /// </summary>
    public async UniTask<bool> DeleteUserDataAsync(string uid, CancellationToken ct = default)
    {
        try
        {
            string nickname = CurrentUser?.Profile?.nickname;

            var updates = new Dictionary<string, object>
            {
                { $"{Users}/{uid}", null },
                { $"{Characters}/{uid}", null },
                { $"{Inventories}/{uid}", null }
            };

            if (!string.IsNullOrEmpty(nickname))
            {
                updates.Add($"{Nicknames}/{nickname}", null);
            }
            await rootRef.UpdateChildrenAsync(updates).AsUniTask().AttachExternalCancellation(ct);

            ClearLocalData();
            UtilDebug.Log($"전체 도메인 데이터 및 닉네임 삭제 완료 (UID: {uid})");
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            UtilDebug.LogError($"유저 데이터 삭제 실패 {ex.Message}");
            return false;
        }
    }
    #endregion

    public void ClearLocalData() => CurrentUser = null;
}