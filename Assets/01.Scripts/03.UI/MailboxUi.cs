using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//담당자 - 정성우

/// <summary>
/// 전체 우편함 팝업 패널 제어 스크립트 (패널UI한태 직접 붙여주는 스크립트)
/// </summary>
public class MailboxUi : MonoBehaviour ,ILoadable
{
    [Header("Ui 패널 안에 들어갈 내용들")]
    [SerializeField] private Transform contentParent; // 스크롤뷰의 content의 트랜스폼
    [SerializeField] private RectTransform contentRectTransform; // content의 RectTransform를 참조
    [SerializeField] private MailItemUi mailItemPrefeb; // 생성할 mailitem의 프리팹
    [SerializeField] private GameObject emptyStateNotion; // 우편이 없을 때 띄울 안내 텍스트/이미지

    [Header("버튼과 알림")]
    [SerializeField] private Button closeButton; // 닫기 버튼
    [SerializeField] private Button OpenButton; // 열기 버튼
    [SerializeField] private GameObject LobbyRedDot; // 우편함 닫혀있을 때 우편이 있다고 알려줄 빨간 알림


    //[제일 핵심] 내가 스폰한 우편UI만을 스폰 디스폰 하기 위해 만든 바구니 역할
    private List<MailItemUi> activeMailItems = new List<MailItemUi>();

    // 메일 아이템 보다는 먼저 되어야 함
    public int LoadOrder => 21;

    private void Awake()
    {
        if(closeButton != null)
        {
            closeButton.onClick.AddListener(CloseWindow);
  
        }
        if(OpenButton != null)
        {
            OpenButton.onClick.AddListener(OpenWindow);
  
        }


        //게임 매니저에서 불러와서 딱 한번만 하게 만들 예정
        ObjcetPoolManager.Instance.RegisterPool<MailItemUi>(enumType.Item_Mail, mailItemPrefeb, 1);

    }

    private void OnEnable()
    {
        // 혹시 모르니 먼저 한 번 빼고 넣기
        MailBoxManager.OnMailboxUpdated -= RefreshUi;
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

        var mailDict = MailBoxManager.Instance.mailDictionary;

        // 우편함이 비웠는지 확인한다.
        bool isEnpty = mailDict.Count == 0;

        if(emptyStateNotion != null) emptyStateNotion.SetActive(isEnpty);
        if(LobbyRedDot != null) LobbyRedDot.SetActive(!isEnpty);

        foreach (var kvp in mailDict)
        {
            //스폰) 풀에서 안전하게 활성화
            MailItemUi item = ObjcetPoolManager.Instance.Spawn<MailItemUi>(enumType.Item_Mail);

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
        var ObjPoolM = ObjcetPoolManager.Instance;

        if(activeMailItems==null||activeMailItems.Count ==0) return; 

        //리스트 요소 삭제 반납시 인덱스 꼬이는 걸 방지하기 위해 역순으로 진행 
        for (int i = activeMailItems.Count-1; i>=0; i--)
        {
            MailItemUi item =activeMailItems[i];
            if(item != null)
            {
                ObjPoolM.Despawn<MailItemUi>(enumType.Item_Mail, item);
            }
        }
        activeMailItems.Clear();

        //content안에 남아있는 오브젝트들이 남아있을 경우를 위한 방어 코드
        if(contentParent !=null)
        {
            for(int i = contentParent.childCount-1; i>=0; i--)
            {
               Transform child = contentParent.GetChild(i);
                if(child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }


    public void CloseWindow()
    {
        gameObject.SetActive(false);
    }

    public void OpenWindow()
    {
        gameObject.SetActive(true);
    }

    public UniTask OnSceneLoadCreate(SceneId scene)
    {
        throw new System.NotImplementedException();
    }

    public void Init(SceneId scene)
    {
        throw new System.NotImplementedException();
    }

    public void OnSceneDestory(SceneId scene)
    {
        throw new System.NotImplementedException();
    }
}
