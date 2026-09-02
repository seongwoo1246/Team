using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 전체 우편함 팝업 패널 제어 스크립트
/// </summary>
public class MailboxUi : MonoBehaviour
{
    [Header("Ui 패널 안에 들어갈 내용들")]
    [SerializeField] private Transform contentParent; // 스크롤뷰의 content의 트랜스폼
    [SerializeField] private GameObject mailItemPrefeb; // 생성할 mailitem의 프리팹
    [SerializeField] private GameObject emptyStateNotion; // 우편이 없을 때 띄울 안내 텍스트/이미지

    [Header("버튼과 알림")]
    [SerializeField] private Button claimAllButton; // 모두 받기 버튼
    [SerializeField] private Button closeButton; // 닫기 버튼
    [SerializeField] private GameObject LobbyRedDot; // 우편함 닫혀있을 때 몇 개 왔는지 알려줄 빨간 알림


    private void Awake()
    {
        if(closeButton != null)
        {
            closeButton.onClick.AddListener(CloseWindow);
        }

        if(claimAllButton != null)
        {
            claimAllButton.onClick.AddListener(OnClickClaimAll);
        }
    }

    private void OnEnable()
    {
        //[중요] 서버 데이터 변경 이벤트 구독
        MailBoxManager.OnMailboxUpdated += RefreshUi;

        //팝업 열릴 시  즉시 Ui 갱신
        RefreshUi();
    }

    private void OnDisable()
    {
        //[중요] 메모리 누수방지를 위해 여기서 해제 해줘야함
        MailBoxManager.OnMailboxUpdated -= RefreshUi;
    }

    private void RefreshUi()
    {
        //1. 기존에 있던 슬롯 Ui 모두 제거
        foreach(Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        var mailDict = MailBoxManager.instance.mailDictionary;

        // 우편함이 비웠는지 확인한다.
        bool isEnpty = mailDict.Count == 0;

        if(emptyStateNotion != null) emptyStateNotion.SetActive(isEnpty);
        if(claimAllButton != null) claimAllButton.interactable = !isEnpty;
        if(LobbyRedDot != null) LobbyRedDot.SetActive(!isEnpty);

        //오브젝트 풀링으로 만들예정
    }


}
