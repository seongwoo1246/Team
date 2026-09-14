using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Models;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using Debug = DebugLogger<IAPManager>;
using System.Runtime.CompilerServices;





#region 영수증 검증 요청 및 응답 DTO(JsonUnity 호환)

[Serializable]
public class GoogleReceiptPayload
{
    public string json;
    public string signature;
}

[Serializable]
public class GoogleReceiptData
{
    public string payLoad;
}

[Serializable]
public class GoogleinnerPayLoad
{
    public string json;
}

[Serializable]
public class GoogleJsonDetails
{
    public string productId;
    public string PurchaseToken;
    public string pakageName;
}

[Serializable]
public class ServerVerifyRequest
{
    public string UserId;
    public string productId;
    public string PurchaseToken;
    public string pakageName;
}

[Serializable]
public class ServerVerifyResponse
{
    public bool Success;
    public string productId;
    public string masage;
}

#endregion


/// <summary>
///  인앱 결제를 구현하려고 만든 클래스
///  In-App Purchasing을 인스톨해서 가상의 결제 시스템을 만들었다.
///  실제로 결제가 되는 것은 아니며 시스템 구현에 초점을 맞추었다.
/// </summary>
public class IAPManager : Singleton<IAPManager> 
{
    //결제 상품의 정의(형식)
    public const string product_Name = "com.yourcompany.game.name";
    public const string product_No_Ads = "com.yourcompany.game.noads";

    [Header("Firebase Config")] // 우리 프로젝트 파이어베이스 주소안에 verifyGooglePurchase같은게 있어서 주소를 적는 느낌
    [SerializeField] private string firebaseVerifyURL = "http://us-centrall-your-project.cloudfuntions.net/verifyGooglePurchase";

    //Unity IAP v5 핵심 컨트롤러
    private StoreController storeController;

    private async void Start()
    {
        //유니티 서비스 및 IAP초기화(v5 표준방식)
        await InitialozeIAPAsync();
    }

    private async UniTask InitialozeIAPAsync()
    {
        try
        {
            //유니티 게이밍 서비스 초기화
            await Unity.Services.Core.UnityServices.InitializeAsync();
            //스토어 컨트롤러 인스턴스 가져오기
            storeController = UnityIAPServices.StoreController();
            //v5 이벤트 리스너 등록
            storeController.OnPurchasePending += OnPurchasePending;
            storeController.OnPurchaseFailed += OnPurchaseFailed;
            //스토어 연결
            await storeController.Connect();
            //상품 목록 들고 오기
            var products = new List<ProductDefinition>
            {
                new ProductDefinition(product_Name,ProductType.Consumable),
                new ProductDefinition(product_No_Ads,ProductType.NonConsumable)
            };
            await storeController.FetchProducts(products);
            Debug.Log("초기화 성공");
        }
        catch(Exception ex)
        {
            Debug.LogWarning($"초기화중 에러 발생  : {ex.Message}");
        }
    }

    /// <summary>
    /// 상품 구매 버튼 클릭시 호출
    /// </summary>
    public async void BuyProduct(string productId)
    {
        if(storeController == null)
        {
            Debug.LogWarning("스토어 초기화가 아직입니다.");
            return;
        }

        Product product = storeController.GetProductById(productId);
        if(product != null&& product.availableToPurchase)
        {
            //v5 구매시작 API
            await storeController.PurchaseProduct(product);
        }
    }

    /// <summary>
    /// v5 이벤트)구매 진행중 영수증의 검증 필요 단계
    /// </summary>
    /// <param name="order"></param>
    private void OnPurchasePending(Order order)
    {
        //백그라운드 비동기 영수증 검증 및 보상지급
        VerifyAndGrantRewardAsync(order,this.GetCancellationTokenOnDestroy()).Forget();
    }

    /// <summary>
    /// v5 이벤트) 구매 실패 처리
    /// </summary>
    /// <param name="order"></param>
    /// <param name="reason"></param>
    private void OnPurchaseFailed(Order order , PurchaseFailureReason reason)
    {
        Debug.LogError($"결제 실패 - 상품 {order.product.definition.id},이유 {reason}");
    }

    private async UniTaskVoid VerifyAndGrantRewardAsync(Order order, CancellationToken Token)
    {
        Product product = order.Product;
        string receiptJson = product.receipt;

        //1. 영주승 토큰 파싱
        if(!TryParseGooleReceipt(receiptJson,out string purchaseToken,out string pakageName))
        {
            Debug.LogError("영수증 파싱 실패");
            return;
        }
        //2. 서버 검증 데이터 구성
        var reData = new ServerVerifyRequest
        {
            UserId = SystemInfo.deviceUniqueIdentifier,
            productId = product.definition.id,
            PurchaseToken = purchaseToken,
            pakageName = pakageName
        };

        //3. 파이어베이스 funtions요청
        ServerVerifyResponse verifyResult = await RequestServerVerificationAsync(reData,Token);

        //4. 서버 검증 성공시 보상 지급 후 최종 승인
        if(verifyResult != null&&verifyResult.Success)
        {
            bool rewardGiven = GrantReward(product.definition.id);
            if(rewardGiven)
            {
                //5. 마감 승인 API
                storeController.ConfirmPurchase(order);
                Debug.Log($"결제 및 보상 완료 {product.definition.id}");
            }
        }
        else
        {
            Debug.LogError($"서버 검증 실패 {verifyResult?.masage}");
        }

    
    }

    private async UniTask<ServerVerifyResponse> RequestServerVerificationAsync(ServerVerifyRequest request, CancellationToken Token)
    {
        string jsonBody = JsonUtility.ToJson(request);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request1 = new UnityWebRequest(firebaseVerifyURL, "POST"))
        {
            request1.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request1.downloadHandler = new DownloadHandlerBuffer();
            request1.SetRequestHeader("Content-Type", "application/json");

            try
            {
                await request1.SendWebRequest().WithCancellation(Token);
                if (request1.result == UnityWebRequest.Result.Success)
                {
                    return JsonUtility.FromJson<ServerVerifyResponse>(request1.downloadHandler.text);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"서버 통신 실패 {ex.Message}");
            }
        }
        return null;
    }


    private bool TryParseGoogleReceipt(string receiptJson, out string purchaseToken, out string packageName)
    {
        purchaseToken = string.Empty;
        packageName = string.Empty;

        try
        {
            var receiptData = JsonUtility.FromJson<GoogleReceiptData>(receiptJson);
            var jnnerPayLoad = JsonUtility.FromJson<GoogleinnerPayLoad>(receiptData.payLoad);
            var jsonDetails = JsonUtility.FromJson<GoogleJsonDetails>(innerPayLoad.json);
            purchaseToken = jsonDetails.PurchaseToken;
            packageName = jsonDetails.pakageName;
            return !string.IsNullOrEmpty(purchaseToken);
        }
        catch
        {
            return false;
        }
    }

    private bool GrantReward(string productId)



}
