using UnityEngine;

public class Test_EquipmentGacha : MonoBehaviour
{
    [Header("장비 데이터")]
    [SerializeField] private EquipmentData[] equipmentDatas;

    [Header("장비 인벤토리")]
    [SerializeField] private EquipmentInventory equipmentInventory;


    // 장비 10개 뽑기
    public void DrawEquipment()
    {
        if (equipmentDatas == null || equipmentDatas.Length == 0)
        {
            Debug.LogWarning("장비 데이터가 없습니다.");
            return;
        }

        for (int i = 0; i < 10; i++)
        {
            // 6개 장비 중 하나 랜덤 선택
            int randomIndex =
                Random.Range(0, equipmentDatas.Length);

            EquipmentData selectedData = equipmentDatas[randomIndex];

            // 장비 옵션 랜덤
            float rollPercent = Random.Range(1f, 100f);

            // 실제 장비 생성
            EquippedItem item = new EquippedItem(selectedData, rollPercent);

            // 인벤토리에 추가
            equipmentInventory.AddItem(item);
        }

        Debug.Log("장비 10회 뽑기");
    }
}