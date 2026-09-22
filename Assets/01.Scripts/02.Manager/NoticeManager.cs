using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Debug = DebugLogger<NoticeManager>;
//담당자 - 정성우

#region 공지사항 데이터 모델 ( 유니티JsonUtility 호환)
[SelectionBase]
public class NoticeItem
{
    public int id; //공지 ID
    public string title;
    public string content;
    public string imageUrl; // 공지 사진 이미지URL
    public string linkUrl; // 링크 URL
}
[SelectionBase]
public class NoticeData
{
    public bool isMaintenance;
    public string maintenanceMassage;
    public List<NoticeItem> notices;
}

#endregion



/// <summary>
/// 공지 사항을 전달 해주기 위해 만든 매니저 
/// </summary>
public class NoticeManager : MonoBehaviour
{
    [Header("공지 UI References")]
    [SerializeField] private GameObject noticePopupUi;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private RawImage noticeRawImage;
    [SerializeField] private Button linkBtn;
    [SerializeField] private Button closeBtn;

  

    private NoticeData currentNoticeData;
    private Texture2D downloadedTexture;

    private async void Start()
    {
        
        
            // 씬 시작시 토큰 생성(씬 파괴시 메모리 누수 방지)
            var token = this.GetCancellationTokenOnDestroy();
            await FetchAndShowNoticeAsync(token);
      
       
       
    }
  
    /// <summary>
    /// 서버에서 공지 데이터를 불러와 UI를 출력하는 메인 비동기 로직
    /// </summary>
    public async UniTask FetchAndShowNoticeAsync(CancellationToken token)
    {
        Debug.Log("0");
        // 1. json 데이터 요청
        string jsonText = LoadLocalNoticeJson();
        if (string.IsNullOrEmpty(jsonText))
        {
            Debug.Log("1");
            noticePopupUi.SetActive(false);
            return;
        }

        //2. json파싱
        currentNoticeData = JsonUtility.FromJson<NoticeData>(jsonText);
        
        // 점검 및 공지 데이터 유효성 체크
        if(currentNoticeData == null || currentNoticeData.notices == null ||currentNoticeData.notices.Count == 0)
        {
            Debug.Log("2");
            noticePopupUi.SetActive(false);
            return;
        }

        if(IsNoticeHiddenToday())
        {
            Debug.Log("3");
            noticePopupUi.SetActive(false);
            return;
        }

       

        //3. 서버 점검 상태 처리 (최우선 확인 사항)
        if (currentNoticeData.isMaintenance)
        {
            Debug.Log("4");
            ShowMaintenancePopup();
            return;
        }

        // 4. 공지사항이 없는 경우 종료
        if(currentNoticeData.notices ==  null||currentNoticeData.notices.Count ==0)
        {
            Debug.Log("5");
            noticePopupUi.SetActive(false);
            return;
        }

        // 5. 첫 번째 공지사항 데이터 세팅 (다중 공지 확상 가능)
        NoticeItem firstNotice = currentNoticeData.notices[0];

        titleText.text = firstNotice.title;
        contentText.text = firstNotice.content;

        // 6. 이미지URL이 있을 경우 비동기 다운로드 및 적용
        if(!string.IsNullOrEmpty(firstNotice.imageUrl))
        {
            downloadedTexture = await DownloadTextureAsync(firstNotice.imageUrl,token);
            if(downloadedTexture != null&&noticeRawImage != null)
            {
                noticeRawImage.texture = downloadedTexture;
                noticeRawImage.gameObject.SetActive(true);
            }
        }
        Debug.Log("6");
        //7. 외부 웹 링크 버튼 이벤트 바인딩
        linkBtn.onClick.RemoveAllListeners();
        if(!string.IsNullOrEmpty(firstNotice.linkUrl))
        {
            linkBtn.gameObject.SetActive(true);
            linkBtn.onClick.AddListener(() => Application.OpenURL(firstNotice.linkUrl));
        }
        else
        {
            linkBtn.gameObject.SetActive(false) ;
        }

        //8. 닫기 버튼 설정
        closeBtn.onClick.RemoveAllListeners();
        closeBtn.onClick.AddListener(CloseNoticeUI);
       
        //Ui 활성화
        noticePopupUi.SetActive(true);
        Debug.Log("끝");
    }




    /// <summary>
    /// 공지 이미지 전용 다운로드 및 메모리 로드
    /// </summary>
    /// <param name="url"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async UniTask<Texture2D> DownloadTextureAsync(string url, CancellationToken token)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url)) 
        {
            await request.SendWebRequest().WithCancellation(token);
            if (request.result == UnityWebRequest.Result.Success)
            {
                return  DownloadHandlerTexture.GetContent(request);
            }
        }
        return null;
    }

    /// <summary>
    /// 점검 팝업 노출 처리
    /// </summary>
    private void ShowMaintenancePopup()
    {
        NoticeItem firstNotice = currentNoticeData.notices[0]; 
        titleText.text = firstNotice.title;
        contentText.text = firstNotice.content;
        if(noticeRawImage != null)  noticeRawImage.gameObject.SetActive(true);
        if(linkBtn !=null) linkBtn.gameObject.SetActive(true);

        noticePopupUi.SetActive(true);
    }

    /// <summary>
    /// Ui 닫기 및 다운로드된 텍스쳐 동적 메모리 해제(최적화 핵심)
    /// </summary>
    private void CloseNoticeUI()
    {
        noticePopupUi.SetActive(false );

        //동적으로 할당받은 이미지 텍스쳐를 즉시 언로드 (GC 부담 완화)
        if(downloadedTexture != null)
        {
            //웹에서 받아온 텍스처는 자동으로 해제를 못해 메모리 누수가 있을 수 있기에 직접 제거를 해야함
            Destroy(downloadedTexture);
            downloadedTexture = null;
        }
    }

    #region 저장 불러오기 기능 추가
    private string LocalNoticePath => Path.Combine(Application.persistentDataPath, "NoticeData.json");
    private const string HIDE_NOTICE_KEY = "Notice_Hide_Date";


    public string LoadLocalNoticeJson()
    {
        if(File.Exists(LocalNoticePath))
        {
            return File.ReadAllText(LocalNoticePath);
        }

        TextAsset defultJson = Resources.Load<TextAsset>("NoticeData");
        return defultJson != null ? defultJson.text : null;
    }

    public void SaveNoticeJson(NoticeData noticeData)
    {
        string jsonText = JsonUtility.ToJson(noticeData,true);
        File.WriteAllText(LocalNoticePath, jsonText);
        Debug.Log($"저장 경로 : {LocalNoticePath}");
    }


    /// <summary>
    /// 오늘 하루 보지 않기 처리하는 함수
    /// </summary>
    public void OnclickHideToday()
    {
        string todayStr = System.DateTime.Now.ToString("yyyyMMdd");
        PlayerPrefs.SetString(HIDE_NOTICE_KEY, todayStr);
        PlayerPrefs.Save();

        CloseNoticeUI();
    }

    private bool IsNoticeHiddenToday()
    {
        if(!PlayerPrefs.HasKey(HIDE_NOTICE_KEY)) { return false; }

        string saveData = PlayerPrefs.GetString(HIDE_NOTICE_KEY);
        string todayStr = System.DateTime.Now.ToString("yyyyMMdd");

        return saveData == todayStr;
       
    }
    #endregion
}
