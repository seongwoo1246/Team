
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
//담당자 - 정성우



/// <summary>
/// 우편 목록 내부에 들어갈 프리팹 바인딩 스크립트
/// </summary>
public class MailItemUi : MonoBehaviour , IPoolable
{
    [Header("Ui 컴포넌트들")]
    [SerializeField] private TextMeshProUGUI titleText; //우편 제목
    [SerializeField] private TextMeshProUGUI contentText; // 우편 내용
    [SerializeField] private TextMeshProUGUI expireText; // 우편 만료시간
    [SerializeField] private Button claimButton; // 수령 버튼
    [SerializeField] private Image rewardIcon; // 첫 번째 대표 아이템 아이콘


    private string currnetMailId;
    private mailItem currentData;

    // 딱히 빨라야 할 거는 없음 
    public int LoadOrder => 22;

    private void Awake()
    {
        if(claimButton != null)
        {
            //수령버튼 바인딩 (중복방지를 위해 한번 비우고 넣어줌)
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClickClaim);
        }
    
    }

    //풀에서 꺼내질 때 초기화를 진행 
    public void OnSpawn()
    {
        // 잔재 데이터 제거
        titleText.text = string.Empty;
        contentText.text = string.Empty;
        expireText.text = string.Empty;
        // 버튼 상태 초기화
        claimButton.interactable = true;

        gameObject.SetActive(true);
    }

    public void OnDespawn()
    {
       
        currentData = null;
        currnetMailId = string.Empty;
        gameObject.SetActive(false);
    }



    public void Setup(mailItem mail)
    {
        currentData = mail;
        currnetMailId = mail.mailId;
        titleText.text = mail.titile;
        contentText.text = mail.content;

        //만료시간 계산
        if(mail.expireTimestamp>0)
        {
            long remainingSeconds = mail.expireTimestamp - System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int remainingDays = (int)(remainingSeconds / 86400);
            expireText.text = remainingDays > 0 ? $"{remainingDays}일 남음" : "오늘 만료";
        }
        else
        {
            expireText.text = "무제한";
        }

      
    }

    public  void OnClickClaim()
    {
        

        // 클릭 중복 방지
        claimButton.interactable = false;

        bool success = MailBoxManager.Instance.ClaimMailReward(currnetMailId);

        if(!success)
        { 
            claimButton.interactable = true;
        }
        
      

        // 성공시 매니저의 이벤트(OnMailboxUpdated)가 나와서 리스트가 리프레시 되면서 자동으로 사라짐
    }
}
