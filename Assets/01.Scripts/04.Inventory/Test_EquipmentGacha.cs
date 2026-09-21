/**/

/* 공동 작업자 - 송태훈
 * 원본 코드를 최대한 훼손하지 않은 채로 코드를 추가하는 방시긍로 진행
 */

using Cysharp.Threading.Tasks;
using UnityEngine;
using UtilDebug = DebugLogger<Test_EquipmentGacha>;

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
    private bool _isDrawing = false;

    public void DrawEquipment()
    {
        if (_isDrawing) return;
        DrawEquipmentAsync().Forget();
    }

    // 장비 10개 뽑기
    public async UniTaskVoid DrawEquipmentAsync()
    {
        if (equipmentDatas == null || equipmentDatas.Length == 0)
        {
            return;
        }

        // 연결 확인용
        if (equipmentInventory == null)
        {
            UtilDebug.LogError("장비 인벤토리가 연결되지 않았습니다.");
            return;
        }

        if (GoldWallet.Instance == null)
        {
            UtilDebug.LogError("GoldWallet을 찾을 수 없습니다.");
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

        _isDrawing = true;
        System.Threading.CancellationToken ct = this.destroyCancellationToken;
        try
        {
            System.Collections.Generic.List<UniTask> saveTask = new System.Collections.Generic.List<UniTask>(10);
            for (int i = 0; i < 10; i++)
            {
                // 장비 데이터 중 하나 랜덤 선택
                int randomIndex = Random.Range(0, equipmentDatas.Length);
                EquipmentData selectedData = equipmentDatas[randomIndex];

                // 실제 장비 생성 (몬스터 드랍이랑 같은 굴림 범위를 쓰도록 공용 함수로 생성)
                EquippedItem item = EquippedItem.CreateFromDrop(selectedData);

                // 인벤토리에 추가
                equipmentInventory.AddItem(item);

                // 서버 인벤토리 DTO 저장 태스크 준비
                if(UserManager.Instance != null && UserManager.Instance.CurrentUser != null)
                {
                    saveTask.Add(UserManager.Instance.AddEquipmentAsync(item.InstanceId, item.ToDTO(), ct));
                }
            }

            // 서버 RTDB에 뽑은 장비 인벤토티 병렬 동기화
            if(saveTask.Count >0)
            {
                await UniTask.WhenAll(saveTask);
            }

            // 차감된 골드 서버에 즉시 플러시
            if (UserManager.Instance != null)
            {
                // 이벤트 핸들러를 만들어서 처리하던가 해야함
                await UserManager.Instance.UpdateGoldAsync(GoldWallet.Instance.Balance, ct);
            }

            UtilDebug.Log($"{gachaCost}골드를 사용하여 장비 10개 뽑기");
        }
        catch(System.Exception ex)
        {
            UtilDebug.LogError($"가챠 처리 중 오류 발생: {ex.Message}");
        }
        finally
        {
            _isDrawing = false;
            // 가챠 이벤트 발생 종료 후 서버에 전체 데이터 저장
            await GameManager.Instance.FlushGameDataAsync();
        }


    }

    public void CloseNotEnoughGoldPanel()
    {
        if (notEnoughGoldPanel != null)
        {
            notEnoughGoldPanel.SetActive(false);
        }
    }
}