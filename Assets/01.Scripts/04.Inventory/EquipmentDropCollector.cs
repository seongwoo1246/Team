// 작성자: 김주연
/*
몬스터가 장비를 드랍하면(StageManager.EquipmentDropped) 그걸 실제 EquipmentInventory에 넣어주는 연결스크립트

몬스터가 드랍하면 StageManager.EquipmentDropped까지는 이미 잘 올라가는데 그걸 받아서
EquipmentInventory.AddItem()을 불러주는 코드가 없어서 드랍은 되는데 인벤토리에는 하나도 안쌓임

로그인 시 서버 인벤토리 복원 + 드랍 시 서버 저장도 여기서 같이 처리함(RestoreSavedEquipment)
*/
/* 공동 작성자 - 송태훈
 
 */

using Cysharp.Threading.Tasks;
using UnityEngine;
using UtilDebug = DebugLogger<EquipmentDropCollector>;

/// <summary>
/// StageManager.EquipmentDropped를 구독해서, 드랍된 장비를 EquipmentInventory에 그대로 추가한다
/// </summary>
public sealed class EquipmentDropCollector : MonoBehaviour, ILoadable
{
    [Tooltip("드랍된 장비를 넣어줄 인벤토리 (EquipmentInventoryManager 등)")]
    [SerializeField] private EquipmentInventory equipmentInventory;

    public int LoadOrder => 40;
    private bool isBind = false;
    private void Awake()
    {
        SceneLoadManager.Instance.RegisterLoadable(this);
    }
    private void OnEnable()
    {
        TryBindStageManager();
    }

    private void OnDisable()
    {
        UnbindStageManager();
    }

    public UniTask OnSceneLoadCreate(SceneId scene) => UniTask.CompletedTask;

    public void Init(SceneId scene)
    {
        if (scene != SceneId.LobbySceneTest) return;

        TryBindStageManager();
        //RestoreSavedEquipment();
    }
    // 씬 전환마다 SceneLoadManager가 여기를 try/catch 없이 그냥 호출해서,
    // 여기서 예외가 나면 그 뒤 씬 전환 단계(메모리 정리/새 씬 로드/매니저 초기화 등)가 전부 스킵됨.
    // OnDisable이랑 똑같이 구독만 해제하면 됨 - 김주연
    public void OnSceneDestory(SceneId scene)
    {
        UnbindStageManager();
    }

    private void TryBindStageManager()
    {
        if (isBind) return;

        if (ServiceLocator.TryGet<StageManager>(out StageManager _stageManager))
        {
            _stageManager.EquipmentDropped -= OnEquipmentDropped;
            _stageManager.EquipmentDropped += OnEquipmentDropped;
            isBind = true;
        }
    }
    private void UnbindStageManager()
    {
        if(!isBind) return;
        if (ServiceLocator.TryGet<StageManager>(out StageManager _stageManager))
        {
            _stageManager.EquipmentDropped -= OnEquipmentDropped;
        }
        isBind = false;
    }

    /// <summary>
    /// 몬스터가 장비를 드랍하면(파밍/웨이브/보스 상관없이) 인벤토리에 그대로 추가한다
    /// </summary>
    /// <param name="item">드랍된 장비 인스턴스</param>
    private void OnEquipmentDropped(EquippedItem item)
    {
        if (equipmentInventory == null || item == null)
        {
            return;
        }

        equipmentInventory.AddItem(item);

        // 김주연 - 서버 인벤토리에도 저장 (여태 로컬 리스트에만 쌓이고 서버엔 저장 안 되던 문제)
        UserManager.Instance.AddEquipmentAsync(item.InstanceId, item.ToDTO(), this.destroyCancellationToken).Forget();
    }

    #region 김주연 - 서버 저장 인벤토리/장착 상태 복원
    /// <summary>
    /// 로그인 시 서버에서 이미 불러와져 있는 CurrentUser.Inventory.Data.equipments(DTO)를
    /// 실제 EquippedItem으로 복원해서 EquipmentInventory에 채우고, 캐릭터별 장착 상태도 그대로 복원한다.
    /// </summary>
    private void RestoreSavedEquipment()
    {
        try
        {
            RestoreSavedEquipmentInternal();
        }
        catch (System.Exception ex)
        {
            UtilDebug.LogError($"저장된 장비 복원 중 예외 발생 - 인벤토리/장착 상태가 불완전할 수 있음: {ex.Message}");
        }
    }

    private void RestoreSavedEquipmentInternal()
    {
        if (equipmentInventory == null)
        {
            return;
        }

        var user = UserManager.Instance.CurrentUser;
        if (user?.Inventory?.Data?.equipments == null)
        {
            return;
        }

        // 1) 인벤토리 복원 (instanceId 기준으로 나중에 장착 복원할 때 다시 찾아 씀)
        var restoredItems = new System.Collections.Generic.Dictionary<string, EquippedItem>();
        foreach (var pair in user.Inventory.Data.equipments)
        {
            EquipmentData data = DataManager.Instance.GetData<EquipmentData>(pair.Value.dataId);
            if (data == null)
            {
                UtilDebug.LogWarning($"저장된 장비 dataId({pair.Value.dataId})를 EquipmentData에서 찾을 수 없음 - instanceId: {pair.Key}");
                continue;
            }

            EquippedItem item = new EquippedItem(pair.Key, data, pair.Value);
            equipmentInventory.AddItem(item);
            restoredItems[pair.Key] = item;
        }

        // 2) 캐릭터별 장착 상태 복원 (이미 서버에 저장된 상태라 persist: false로 다시 저장 안 함)
        var charDict = user.Characters?.characterDictionary;
        if (charDict == null)
        {
            return;
        }

        foreach (CharacterBase character in CharacterBase.AllCharacters)
        {
            if (character == null || character.StatData == null)
            {
                continue;
            }

            if (!charDict.TryGetValue(character.StatData.Id, out var charData))
            {
                continue;
            }

            foreach (var slotPair in charData.equippedItems)
            {
                if (!System.Enum.TryParse(slotPair.Key, out EquipmentSlot slot))
                {
                    continue;
                }

                if (!restoredItems.TryGetValue(slotPair.Value, out EquippedItem item))
                {
                    continue;
                }

                character.Equip(slot, item, persist: false);
            }
        }
    }
    #endregion
}
