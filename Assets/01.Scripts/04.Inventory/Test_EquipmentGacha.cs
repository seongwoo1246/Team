/*
 작업자 - 홍준호
 상점 장비 뽑기용 스크립트
*/

/* 공동 작업자 - 송태훈
 * 원본 코드를 최대한 훼손하지 않은 채로 코드를 추가하는 방식으로 진행
 */

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
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

    [Header("가챠 결과창")]
    [SerializeField] private GameObject gachaResultPanel;
    [SerializeField] private Transform resultGrid;
    [SerializeField] private GameObject resultSlotPrefab;
    [SerializeField] private UnityEngine.UI.Button resultButton;

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
            {
                notEnoughGoldPanel.SetActive(true);
            }

            return;
        }


        _isDrawing = true;

        System.Threading.CancellationToken ct = this.destroyCancellationToken;

        try
        {
            System.Collections.Generic.List<UniTask> saveTask =
                new System.Collections.Generic.List<UniTask>(10);

            // 이번 가챠에서 뽑은 장비 10개
            System.Collections.Generic.List<EquippedItem> resultItems =
                new System.Collections.Generic.List<EquippedItem>(10);


            for (int i = 0; i < 10; i++)
            {
                // 장비 데이터 중 하나 랜덤 선택
                int randomIndex =
                    Random.Range(0, equipmentDatas.Length);

                EquipmentData selectedData =
                    equipmentDatas[randomIndex];


                // 실제 장비 생성
                // 가챠 전용 등급 가중치 - 하급 없이 중급/상급만 나옴
                EquippedItem item =
                    EquippedItem.CreateFromGacha(selectedData);


                // 가챠 결과창에 표시할 리스트에 추가
                resultItems.Add(item);


                // 인벤토리에 추가
                equipmentInventory.AddItem(item);


                // 서버 인벤토리 DTO 저장 태스크 준비
                if (!UserManager.Instance.IsLocalMode &&
                    UserManager.Instance.CurrentUser != null)
                {
                    saveTask.Add(
                        UserManager.Instance.AddEquipmentAsync(
                            item.InstanceId,
                            item.ToDTO(),
                            ct
                        )
                    );
                }
            }


            // 서버 RTDB에 뽑은 장비 인벤토리 병렬 동기화
            if (saveTask.Count > 0)
            {
                await UniTask.WhenAll(saveTask);
            }


            // 차감된 골드 서버에 즉시 플러시
            if (UserManager.Instance != null)
            {
                // 이벤트 핸들러를 만들어서 처리하던가 해야함
                await UserManager.Instance.UpdateGoldAsync(
                    GoldWallet.Instance.Balance,
                    ct
                );
            }


            // 로컬 모드일 때 디스크 저장 및 강제 플러시
            if (UserManager.Instance != null &&
                UserManager.Instance.IsLocalMode)
            {
                UserManager.Instance.SaveLocalUserData();
            }


            // 가챠 결과창 표시
            await ShowGachaResult(resultItems);


            UtilDebug.Log($"{gachaCost}골드를 사용하여 장비 10개 뽑기");
        }
        catch (System.Exception ex)
        {
            UtilDebug.LogError(
                $"가챠 처리 중 오류 발생: {ex.Message}"
            );
        }
        finally
        {
            _isDrawing = false;

            // 가챠 이벤트 발생 종료 후 서버에 전체 데이터 저장
            await GameManager.Instance.FlushGameDataAsync();
        }
    }


    // 가챠에서 뽑은 장비 10개를 결과창에 표시
    private async UniTask ShowGachaResult(
    System.Collections.Generic.List<EquippedItem> resultItems)
    {
        if (gachaResultPanel == null)
        {
            UtilDebug.LogError("가챠 결과창이 연결되지 않았습니다.");
            return;
        }

        if (resultGrid == null)
        {
            UtilDebug.LogError("가챠 결과 Grid가 연결되지 않았습니다.");
            return;
        }

        if (resultSlotPrefab == null)
        {
            UtilDebug.LogError("가챠 결과 슬롯 프리팹이 연결되지 않았습니다.");
            return;
        }

        // 기존 결과 슬롯 삭제
        for (int i = resultGrid.childCount - 1; i >= 0; i--)
        {
            Destroy(resultGrid.GetChild(i).gameObject);
        }

        // 확인 버튼 비활성화
        if (resultButton != null)
        {
            resultButton.interactable = false;
        }

        // 결과창 먼저 열기
        gachaResultPanel.SetActive(true);

        // 장비를 하나씩 순서대로 표시
        foreach (EquippedItem item in resultItems)
        {
            GameObject slotObject =
                Instantiate(resultSlotPrefab, resultGrid);

            EquipmentInventorySlot slot =
                slotObject.GetComponent<EquipmentInventorySlot>();

            if (slot == null)
            {
                UtilDebug.LogError(
                    "가챠 결과 슬롯 프리팹에 " +
                    "EquipmentInventorySlot이 없습니다."
                );

                continue;
            }

            // 장비 표시
            slot.SetResultItem(item);

            // 다음 장비가 나오기까지 0.15초 대기
            await UniTask.Delay(150);
        }

        // 10개가 모두 나온 후 확인 버튼 활성화
        if (resultButton != null)
        {
            resultButton.interactable = true;
        }
    }


    // 가챠 결과창 닫기
    public void CloseGachaResultPanel()
    {
        if (gachaResultPanel != null)
        {
            gachaResultPanel.SetActive(false);
        }
    }


    // 골드 부족 안내창 닫기
    public void CloseNotEnoughGoldPanel()
    {
        if (notEnoughGoldPanel != null)
        {
            notEnoughGoldPanel.SetActive(false);
        }
    }
}