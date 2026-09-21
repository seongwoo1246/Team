/**/

/* 공동 작업자 - 송태훈
 
 */

using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UtilDebug = DebugLogger<EquipmentInventory>;
public class EquipmentInventory : MonoBehaviour, ILoadable, ISyncable
{
    [SerializeField] private List<EquippedItem> items = new List<EquippedItem>();

    public List<EquippedItem> Items => items;

    // PartyFormationManger(현재 17) 보다 먼저 초기화 되어야 함
    public int LoadOrder => 10;
    private bool _isInitialized = false;
    private void Awake()
    {
        ServiceLocator.Register<EquipmentInventory>(this, ServiceLifetime.Local);
        SceneLoadManager.Instance.RegisterLoadable(this);
        GameManager.Instance.RegisterSyncable(this);
    }

    private void OnDestroy()
    {
        SceneLoadManager.Instance.UnregisterLoadable(this);
        GameManager.Instance.UnregisterSyncable(this);
    }

    #region ILoadable + ISyncable 구현부 - 송태훈
    public UniTask OnSceneLoadCreate(SceneId scene)
    {
        return UniTask.CompletedTask;
    }

    public void Init(SceneId scene)
    {
        if (scene != SceneId.LobbySceneTest) return;
        if (_isInitialized) return;

        LoadInventoryFromServer();
        _isInitialized = true;
    }

    public void OnSceneDestory(SceneId scene)
    {
        SyncToUserMemory();
        items.Clear();
        _isInitialized = false;
    }
    public void SyncToUserMemory()
    {
        var invData = UserManager.Instance.CurrentUser?.Inventory?.Data;
        if (invData == null) return;

        invData.equipments.Clear();
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item != null && !string.IsNullOrEmpty(item.InstanceId))
            {
                invData.equipments[item.InstanceId] = item.ToDTO();
            }
        }
    }
    #endregion

    private void LoadInventoryFromServer()
    {
        items.Clear();

        UserInfo user = UserManager.Instance.CurrentUser;
        if (user?.Inventory?.Data?.equipments == null)
        {
            UtilDebug.LogError("서버 유저 인벤토리 비어 있음"); return;
        }

        Dictionary<string, EquippedItem> restoreMap = new Dictionary<string, EquippedItem>();

        // DTO -> EquippedItem 인스턴스 복원
        foreach (var pair in user.Inventory.Data.equipments)
        {
            string instnaceId = pair.Key;
            EquipmentSaveDTO dto = pair.Value;

            EquipmentData data = DataManager.Instance.GetData<EquipmentData>(dto.dataId);
            if (data == null)
            {
                UtilDebug.LogWarning($"장비 데이터(Id: {dto.dataId})를 DataManager에서 찾을 수 없음");
                continue;
            }

            EquippedItem item = new EquippedItem(instnaceId, data, dto);
            items.Add(item);
            restoreMap[instnaceId] = item;
        }
        UtilDebug.Log($"서버 인벤토리 복원 완료: 총 {items.Count}개");
    }

    public bool TryGetItem(string instanceId, out EquippedItem item)
    {
        item = items.Find(x => x.InstanceId == instanceId);
        return item != null;
    }

    #region 인벤토리 조작 - 홍준호? 김주연?

    // 장비 추가
    public void AddItem(EquippedItem item)
    {
        if (item == null)
            return;

        items.Add(item);
    }

    // 장비를 인벤토리에서 제거
    public bool RemoveItem(EquippedItem item)
    {
        if (item == null)
            return false;

        return items.Remove(item);
    }

    // 장비를 종류 별로 불러옴
    public List<EquippedItem> GetItemsBySlot(EquipmentSlot slot)
    {
        List<EquippedItem> result = new List<EquippedItem>();

        for (int i = 0; i < items.Count; i++)
        {
            EquippedItem item = items[i];

            if (item != null && item.Data != null && item.Data.Slot == slot)
            {
                result.Add(item);
            }
        }

        return result;
    }

    // 무기도 직업별로 분류하기
    public List<EquippedItem> GetItemsBySlotAndAttackType(EquipmentSlot slot, AttackType attackType)
    {
        List<EquippedItem> result = new List<EquippedItem>();

        for (int i = 0; i < items.Count; i++)
        {
            EquippedItem item = items[i];

            if (item == null || item.Data == null)
                continue;

            if (item.Data.Slot != slot)
                continue;

            if (item.Data.AllowedAttackType != attackType)
                continue;

            result.Add(item);
        }

        return result;
    }
    #endregion
}