using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 전체 우편함 팝업 패널 제어 스크립트 (패널UI한태 직접 붙여주는 스크립트)
/// </summary>
public class MailboxUi : MonoBehaviour
{
    [Header("Ui 패널 안에 들어갈 내용들")]
    [SerializeField] private Transform contentParent; // 스크롤뷰의 content의 트랜스폼
    [SerializeField] private MailItemUi mailItemPrefeb; // 생성할 mailitem의 프리팹
    [SerializeField] private GameObject emptyStateNotion; // 우편이 없을 때 띄울 안내 텍스트/이미지

    [Header("버튼과 알림")]
    [SerializeField] private Button claimAllButton; // 모두 받기 버튼
    [SerializeField] private Button closeButton; // 닫기 버튼
    [SerializeField] private GameObject LobbyRedDot; // 우편함 닫혀있을 때 몇 개 왔는지 알려줄 빨간 알림


    //[제일 핵심] 내가 스폰한 우편UI만을 스폰 디스폰 하기 위해 만든 바구니 역할
    private List<MailItemUi> activeMailItems = new List<MailItemUi>();

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

    private void Start()
    {
        //게임 매니저에서 불러와서 딱 한번만 하게 만들 예정
        ObjcetPoolManager.instance.RegisterPool<MailItemUi>(enumType.Item, mailItemPrefeb, 10);
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
        ClearMailList();
    }


    /// <summary>
    /// 우편함을 열 때 마다 안에 UI를 전부 정리하고 다시 활성화 하는 방식
    /// </summary>
    private void RefreshUi()
    {
        //1. 기존에 있던 슬롯 Ui 모두 제거
        ClearMailList();

        var mailDict = MailBoxManager.instance.mailDictionary;

        // 우편함이 비웠는지 확인한다.
        bool isEnpty = mailDict.Count == 0;

        if(emptyStateNotion != null) emptyStateNotion.SetActive(isEnpty);
        if(claimAllButton != null) claimAllButton.interactable = !isEnpty;
        if(LobbyRedDot != null) LobbyRedDot.SetActive(!isEnpty);

        foreach (var kvp in mailDict)
        {
            //스폰) 풀에서 안전하게 활성화
            MailItemUi item = ObjcetPoolManager.instance.Spawn<MailItemUi>(enumType.Item);

            if(item != null)
            {
                item.transform.SetParent(contentParent, false);
                item.Setup(kvp.Value);

                //내가 스폰한 아이템 리스트에 보관
                activeMailItems.Add(item);
            }
        }
    }

    private void ClearMailList()
    {
        var ObjPoolM = ObjcetPoolManager.instance;

        for (int i = 0; i<activeMailItems.Count; i++)
        {
            if(activeMailItems[i] != null)
            {
                ObjPoolM.Despawn<MailItemUi>(enumType.Item, activeMailItems[i]);
            }
        }
        activeMailItems.Clear();
    }


    private void OnClickClaimAll()
    {
        MailBoxManager.instance.ClaimAllMails();
    }

    private void CloseWindow()
    {
        gameObject.SetActive(false);
    }
}
