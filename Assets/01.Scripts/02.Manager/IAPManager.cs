using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using Debug = DebugLogger<IAPManager>;
using Firebase.Functions;

// 중요한 건 구글 플레이 콘솔 연동이 안되있어서 파이어 베이스 연결해서 웹 주소를 얻어서 바꿔 넣어도 상품 등록이 안됌 (개발자 아이디 만드려고 하니 등록비 25달러라고 함 ㅎㄷㄷ)
#region 백엔드에 들어갈 코드(예시)
/*
 
const function = require("firebase-functions");
const {google} = require("googleapis");

// 구글 서비스 계정 인증 정보 설정
const auth = new google.auth.GoogleAuth
({
    keyFile : "./google-services-key.json", // 구글 플레이 콘솔에서 받은 서비스 계정 키 파일
    scopes : ["https://www.googleapis.com/auth/androidpublisher"],
});

exports.verifyGooglePurchase = funtions.https.onRequest(async (req,res) =>
{
    //유니티에서 보낸 데이터 받기
    const {productId,purchaseToken, packageName} =  req.body;
try
{
const playDeveloperApi = google.androidpublisher({version : "v3",auth});
//구글 서버에 영수증(purchaseToken) 진짜 여부 조회
const result = await playDeveloperApi.Purchases.products.get({
packageName : packageName,
productId : productId,
token : purchaseToken,
});

//결제 상태 확인 (0: 결제 완료)
if(result.data.purchaseState == 0)
{
//검증 성공 유니티로 성공 응답 전송
return res.status(200).json
({
Success : flase,
productId : productId,
});
}
}
catch (error)
{
console.error('검증 에러 :",error);
return res.status(500).json
({
Success : false,
productId : productId,
});

 */
#endregion


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
///  GooglePlayConsole의 상품 등록을 해야함
/// </summary>
public class IAPManager : Singleton<IAPManager> 
{
    //결제 상품의 정의(형식)
    public const string product_Name = "com.yourcompany.game.name";
    public const string product_No_Ads = "com.yourcompany.game.noads";

    [Header("Firebase Config")] // 현재는 임시 주소고 실제 배포된 파이어베이스 클라우드 펑션스의 HTTP요청URL을 적어야함
    [SerializeField] private string firebaseVerifyURL = "http://us-centrall-your-project.cloudfuntions.net/verifyGooglePurchase";

    //Unity IAP v5 핵심 컨트롤러
    private StoreController storeController;

    private async void Start()
    {
        //유니티 서비스 및 IAP초기화(v5 표준방식)
        await InitialozeIAPAsync();
    }

    /// <summary>
    /// 유니티 IAP를 초기화하고 판매할 상품을 스토어에 등록을 하는 함수
    /// </summary>
    /// <returns></returns>
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
             storeController.FetchProducts(products);
            Debug.Log("초기화 성공");
        }
        catch(Exception ex)
        {
            Debug.LogWarning($"초기화중 에러 발생  : {ex.Message}");
        }
    }

    /// <summary>
    /// 상품 구매 버튼 클릭시 나와서 GooglePlay 구매창을 띄워주는 함수
    /// </summary>
    public  void BuyProduct(string productId)
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
             storeController.PurchaseProduct(product);
        }
    }

    /// <summary>
    /// v5 이벤트) 사용자가 결제를 완료하면 호출되어 백엔드 서버와 영수증 검증을 요청하는 비동기 함수실행
    /// </summary>
    /// <param name="order"></param>
    private void OnPurchasePending(PendingOrder order)
    {
        //백그라운드 비동기 영수증 검증 및 보상지급
        VerifyAndGrantRewardAsync(order,this.GetCancellationTokenOnDestroy()).Forget();
    }

    /// <summary>
    /// v5 이벤트) 구매 실패 처리
    /// </summary>
    /// <param name="order"></param>
    /// <param name="reason"></param>
    private void OnPurchaseFailed(FailedOrder order)
    {
        Debug.LogError($"결제 실패 - 이유 {order.FailureReason}");
    }

    /// <summary>
    /// 결제 영수증에서 구매 토큰을 뽑아와서 백엔드 서버와 정말 결제한 건지 확인 후 아이템을 지급하는 함수 /(빌드할 시) 개발자 아이디가 없을 시 여기를 주석처리하고 바로 보상을 주게 될 수 도 있음
    /// </summary>
    /// <param name="order"></param>
    /// <param name="Token"></param>
    /// <returns></returns>
    private async UniTaskVoid VerifyAndGrantRewardAsync(PendingOrder order, CancellationToken Token)
    {
       Product product = order.CartOrdered.Items().FirstOrDefault()?.Product;
        string receiptJson = order.Info.Receipt;

        //1. 영주승 토큰 파싱
        if (!TryParseGoogleReceipt(receiptJson,out string purchaseToken,out string pakageName))
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

    /// <summary>
    /// 백엔드 서버로 Rest API HTTP요청을 보내는 통신 함수
    /// </summary>
    /// <param name="request"></param>
    /// <param name="Token"></param>
    /// <returns></returns>
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


    /// <summary>
    /// 구글 영수증(json)을 해독하여 나머지 2개쪽으로 파싱 하는 함수
    /// </summary>
    /// <param name="receiptJson">영수증Json</param>
    /// <param name="purchaseToken"></param>
    /// <param name="packageName"></param>
    /// <returns></returns>
    private bool TryParseGoogleReceipt(string receiptJson, out string purchaseToken, out string packageName)
    {
        purchaseToken = string.Empty;
        packageName = string.Empty;

        try
        {
            var receiptData = JsonUtility.FromJson<GoogleReceiptData>(receiptJson);
            var innerPayLoad = JsonUtility.FromJson<GoogleinnerPayLoad>(receiptData.payLoad);
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

    /// <summary>
    /// 검증이 완료된후 게임 재화 및 아이템을 실제 유저에게 줄 때 사용되는 함수
    /// </summary>
    /// <param name="productId">줘야 하는 물건ID</param>
    /// <returns></returns>
    private bool GrantReward(string productId)
    {
        // 실제 게임 재화가 지급되느 함수 호출
        return true;
    }



}
