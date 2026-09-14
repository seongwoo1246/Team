/*
몬스터가 장비를 드랍하면(StageManager.EquipmentDropped) 그걸 실제 EquipmentInventory에 넣어주는 연결스크립트

몬스터가 드랍하면 StageManager.EquipmentDropped까지는 이미 잘 올라가는데 그걸 받아서
EquipmentInventory.AddItem()을 불러주는 코드가 없어서 드랍은 되는데 인벤토리에는 하나도 안쌓임
*/

using UnityEngine;

/// <summary>
/// StageManager.EquipmentDropped를 구독해서, 드랍된 장비를 EquipmentInventory에 그대로 추가한다
/// </summary>
public sealed class EquipmentDropCollector : MonoBehaviour
{
    [Tooltip("드랍된 장비를 넣어줄 인벤토리 (EquipmentInventoryManager 등)")]
    [SerializeField] private EquipmentInventory equipmentInventory;

    private StageManager _stageManager;

    private void OnEnable()
    {
        _stageManager = StageManager.Instance;
        if (_stageManager != null)
        {
            _stageManager.EquipmentDropped += OnEquipmentDropped;
        }
    }

    private void OnDisable()
    {
        if (_stageManager != null)
        {
            _stageManager.EquipmentDropped -= OnEquipmentDropped;
        }
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
