using UnityEngine;

public class Test_EquipmentGacha : MonoBehaviour
{
    [Header("장비 데이터")]
    [SerializeField] private EquipmentData[] equipmentDatas;

    [Header("장비 인벤토리")]
    [SerializeField] private EquipmentInventory equipmentInventory;

    [Header("가챠 비용")]
    [SerializeField] private double gachaCost = 1000d;

    [Header("골드 부족 안내창")]
    [SerializeField] private GameObject notEnoughGoldPanel;


    // 장비 10개 뽑기
    public void DrawEquipment()
    {
        if (equipmentDatas == null || equipmentDatas.Length == 0)
        {
            return;
        }

        // 연결 확인용
        if (equipmentInventory == null)
        {
            Debug.LogWarning("장비 인벤토리가 연결되지 않았습니다.");
            return;
        }

        if (GoldWallet.Instance == null)
        {
            Debug.LogWarning("GoldWallet을 찾을 수 없습니다.");
            return;
        }
        // 연결 확인용 //



        // 골드가 충분한지 확인하고 1000골드 차감
        if (!GoldWallet.Instance.TrySpend(gachaCost))
        {
            if (notEnoughGoldPanel != null) 
                notEnoughGoldPanel.SetActive(true);
            

            return;
        }

        for (int i = 0; i < 10; i++)
        {
            // 장비 데이터 중 하나 랜덤 선택
            int randomIndex = Random.Range(0, equipmentDatas.Length);
            EquipmentData selectedData = equipmentDatas[randomIndex];

            // 실제 장비 생성 (몬스터 드랍이랑 같은 굴림 범위를 쓰도록 공용 함수로 생성)
            EquippedItem item = EquippedItem.CreateFromDrop(selectedData);

            // 인벤토리에 추가
            equipmentInventory.AddItem(item);
        }

        Debug.Log($"{gachaCost}골드를 사용하여 장비 10개 뽑기");
    }

    public void CloseNotEnoughGoldPanel()
    {
        if (notEnoughGoldPanel != null)
        {
            notEnoughGoldPanel.SetActive(false);
        }
    }
}