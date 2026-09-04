using Firebase.Database;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using UnityEngine;
using Dedug = DebugLogger<Shop>;


//아이템 등급에 관련된 enum 필요없어질 시 천장 달성시 원하는 아이템을 주는 식으로 변경
public enum ItemRarity
{
    Nomal,
    Rare,
    Legendary

}

public enum ShopType
{
    Fixed, // 고정형 상점
    Gacha // 가챠형 상점
}

public enum CurrencyType
{
    Gold,  //일반 재화
    Diamond//유료재화
}


/// <summary>
/// 가챠 항목 1개의 정보
/// </summary>
[SelectionBase]
public class GachaRewardItem
{
    public int itemCode; // 아이템 코드
    public int amount;  // 지급 수량
    public int weight; // 가중치
    public ItemRarity rarity; // 아이템 등급
}

/// <summary>
/// 상점 상품 전체 클래스
/// </summary>
[SelectionBase]
public class ShopProduct
{
    public int productId;
    public string productName;
    public ShopType shoptype;
    public CurrencyType currencyType;
    public double price;

    public int fixedItemCode;
    public int fixedItemAmount;

    public List<GachaRewardItem> gachaList = new List<GachaRewardItem>();

    //서버 역직렬화를 위한 기본자
    public ShopProduct() { }
}

/// <summary>
/// 상점 기능을 구현한 스크립트 / 가챠형과 고정형을 같이 만들 예정
/// </summary>
public class Shop : Singleton<Shop>
{
    private DatabaseReference dbRef;
    private string currentUserId = ""; // 나중에는 Auth UID사용


    public const int Max_Gacha_count = 100; //천장 횟수

    //상점 상품들을 들고 있을 딕셔너리(상품id,상점상품클래스)
    public Dictionary<int, ShopProduct> productDic { get; private set; } = new Dictionary<int, ShopProduct>();

    protected override void Awake()
    {
        base.Awake();
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
    }

    #region 가챠를 위한 코드들
    /// <summary>
    /// 가챠 돌리는 기능 가챠 후 금액 차감후 서버에 저장
    /// </summary>
    /// <param name="product">상품</param>
    /// <param name="drawCount">가챠 돌리는 횟수 (10연 가챠등을 위해 추가)</param>
    /// <returns></returns>
    public async Task<List<GachaRewardItem>> DrawGachaAsync(ShopProduct product , int drawCount)
    {
        double totalPrice = product.price * drawCount;

        // 파이어 베이스를 통해 안전하게 재화 차감
        bool isDeducted = await DeductCurrencyOnServerAsync(product.currencyType , totalPrice);
        if(!isDeducted)
        {
            Debug.LogWarning("서버 재화 차감 실패 : 재화가 부족하거나 통신 장애가 있습니다.");
            return null;
        }

        //서버에서 현재 천장 스택 불러오기
        int currentGachaCount = await LoadUserGachaCountAsync();

        List<GachaRewardItem> results = new List<GachaRewardItem>();

        for(int i = 0; i < drawCount; i++)
        {
            currentGachaCount++;

            GachaRewardItem reward;

            // 천장 도달 여부 확인
            if(currentGachaCount>=Max_Gacha_count)
            {
                //천장 발동시 전설등급 리스트에서 한개 던져주기(혹은 다른 버전)
                reward = GetGuaranteedLegendaryItem(product.gachaList);
                currentGachaCount = 0;
                Debug.Log("천장 발동!!");
            }
           else
            {
                // 일반 가챠 진행
                reward = CalculateGachaReward(product.gachaList);

                // 우연히 최종 등급이 나오면 스택 초기화
                if(reward != null&& reward.rarity == ItemRarity.Legendary)
                {
                    currentGachaCount = 0;
                    Debug.Log("자연산 레전드 출현 스택 초기화!!");
                }
            }

            if(reward != null)
            {
                results.Add(reward);
            }
        }

        // 연산된 최종 천장 스택과 획득 아이템 정보를 서버로 보냄
        await SaveGachaResultsToServerAsync(currentGachaCount, results);
        return results; //UI 연출용 , 획득 결과 목록 반환
    }

     


    /// <summary>
    /// 가중치 기반 랜덤 가챠 알고리즘
    /// </summary>
    /// <param name="gachaList"></param>
    /// <returns></returns>
    private GachaRewardItem CalculateGachaReward(List<GachaRewardItem> gachaList)
    {
        if (gachaList == null || gachaList.Count == 0) return null;

        //모든 아이템의 가중치를 합산
        int totalWeigth = 0;
        for (int i = 0; i < gachaList.Count; i++)
        {
            totalWeigth += gachaList[i].weight;
        }

        //0부터 전체가중치-1 사이의 무작위 숫자 한개 뽑기
        int randomValue = UnityEngine.Random.Range(0, totalWeigth);

        //뽑힌 무작위 숫자가 어느 아이템 구간에 들어가는에 확인
        int currentSum = 0;
        for(int i = 0;i < gachaList.Count;i++)
        {
            currentSum += gachaList[i].weight;
            
            //무작위 숫자가 현재 누적합보다 작다면 이 아이템 당첨
            if(randomValue < currentSum)
            {
                return gachaList[i];
            }
        }
        return gachaList[0]; // 예외상황을 대비한 기본값
    }

    private GachaRewardItem GetGuaranteedLegendaryItem(List<GachaRewardItem> gachaList)
    {
        List<GachaRewardItem> legendaries = gachaList.FindAll(x=>x.rarity == ItemRarity.Legendary);

        if(legendaries.Count == 0)
        {
            // 전설 등급이 없을 시 그냥 뽑기 실행
            return CalculateGachaReward(gachaList);
        }

        int randomIndex = UnityEngine.Random.Range(0,legendaries.Count);
        return legendaries[randomIndex];
    }
    #endregion

    /// <summary>
    /// 고정형 상품 구매 버튼
    /// </summary>
    /// <param name="productId"></param>
    /// <returns></returns>
    public async Task PurchaseProductAsync(int productId)
    {
        //존재하는 상품인지 확인하기 
        if (!productDic.TryGetValue(productId, out ShopProduct product))
        {
            Dedug.LogError("존재하지 않는 상품입니다.");
            return;
        }

        // 재화는 충분한가 확인
        bool hasEnoughMoney = CheckUserCurrency(product.currencyType, product.price);
        if (!hasEnoughMoney)
        {
            Dedug.LogWarning("재화가 부족합니다.");
            return;
        }

        // 재화 차감 처리 (서버와 연동해서 해야함)
        bool deductSuccess = await DeductCurrencyOnServerAsync(product.currencyType, product.price);
        if (!deductSuccess) return;


        //고정형 상품 수령 
        GrantItem(product.fixedItemCode, product.fixedItemAmount);
        Dedug.Log($"[고정 상품 구매 성공] 아이템 Id {product.fixedItemCode} x{product.fixedItemAmount}");



    }

    #region 서버 통신및 인벤토리 연동

    /// <summary>
    /// 재화는 충분한가 확인
    /// </summary>
    /// <param name="type">재화 종류</param>
    /// <param name="price">재화 가격</param>
    /// <returns></returns>
    private bool CheckUserCurrency(CurrencyType type , double price)
    {
        GoldWallet wallent = GoldWallet.instance;
        if(CurrencyType.Gold == type&& wallent.Balance>=price)
        {
            return true;
        }
        // 유료 재화(다이아)가 활성화 되면 넣을 부분
        //else if (CurrencyType.Diamond == type&&다이아 확인 해서 부족한가 아닌가 확인 하는 bool)
        //{
        //    return true;
        //}
        else
        {
            return false;
        }      
    }

    /// <summary>
    /// 파이어 베이스 트랜잭션 기반 서버 재화 차감 함수
    /// </summary>
    /// <param name="type">결재 타입</param>
    /// <param name="price">결재 금액</param>
    /// <returns></returns>
    private async Task<bool> DeductCurrencyOnServerAsync(CurrencyType type, double price)
    {
        DatabaseReference userGoldRef = dbRef.Child("users").Child(currentUserId).Child(type.ToString());

        bool isSuccess = false;

        //Transaction을 이용하면 동시에 여러 요청이 들어가도 서버 재화 데이터 오염 방지
        await userGoldRef.RunTransaction(MutableData =>
        {
            double currentBalance = Convert.ToDouble(MutableData.Value ?? 0);

            // 잔액 부족시 트랜잭션 중단
            if (currentBalance < price)
            {
                return TransactionResult.Abort();
            }

            // 차감 처리
            MutableData.Value = currentBalance - price;
            isSuccess = true;
            return TransactionResult.Success(MutableData);
        });

        // 클라이언트 연동) 서버가 차감 성공시만 실행해서 UI및 로컬에 저장된 수치 차감 업데이트
        if(isSuccess)
        {
            if(type == CurrencyType.Gold && GoldWallet.instance != null )
            {
                GoldWallet.instance.TrySpend(price);
            }
        }
        return isSuccess;
            
    }

    /// <summary>
    /// 유저의 현재 천장 스택 로드
    /// </summary>
    /// <returns>없으면 0부터 시작</returns>
    private async Task<int> LoadUserGachaCountAsync()
    {
        DataSnapshot snapshot = await dbRef.Child("users").Child(currentUserId).Child("gachaStack").GetValueAsync();
        if(snapshot.Exists)
        {
            return Convert.ToInt32(snapshot.Value);
        }
        return 0;
    }

    /// <summary>
    /// 가챠 결과 및 갱신된 천장 스택을 파이어베이스 서버로 일괄 저장
    /// </summary>
    /// <param name="newGachaCount">갱신된 천장 스택</param>
    /// <param name="rewards">가챠 결과</param>
    /// <returns></returns>
    private async Task SaveGachaResultsToServerAsync(int newGachaCount , List<GachaRewardItem> rewards)
    {
        Dictionary<string , object> updates = new Dictionary<string , object>();

        //천장 스택 저장 경로 지정
        updates[$"/users/{currentUserId}/gachaStack"] = newGachaCount;

        // 획득한 보상을 인벤토리 서버 누적 저장 경로 지정
        foreach(var reward in rewards)
        {
            string itemPath = $"users/{currentUserId}/inventory/{reward.itemCode}";
            // 배치 업데이트용 처리
            updates[itemPath] = reward.amount;

            GrantItem(reward.itemCode, reward.amount);
        }

        //배치 통신 한번에 결과 저장
        await dbRef.UpdateChildrenAsync(updates);
    }

    private void GrantItem(int itemCode, int amount)
    {
        //유저 인벤토리로 아이템을 보냄
        //UserInventory.instance.GetItem(itemCode,아이템 스프라이트,amount)
    }

    #endregion
}
