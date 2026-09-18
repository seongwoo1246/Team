// 작성자: 김주연
/*
몬스터가 장비를 드랍하면(StageManager.EquipmentDropped) 그걸 실제 EquipmentInventory에 넣어주는 연결스크립트

몬스터가 드랍하면 StageManager.EquipmentDropped까지는 이미 잘 올라가는데 그걸 받아서
EquipmentInventory.AddItem()을 불러주는 코드가 없어서 드랍은 되는데 인벤토리에는 하나도 안쌓임
*/
/* 공동 작성자 - 송태훈
 
 */

using Cysharp.Threading.Tasks;
using UnityEngine;

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
    }
    public void OnSceneDestory(SceneId scene)
    {
        throw new System.NotImplementedException();
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
    }
}
