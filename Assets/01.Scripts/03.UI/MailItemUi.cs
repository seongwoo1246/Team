using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;




/// <summary>
/// 우편 목록 내부에 들어갈 프리팹 바인딩 스크립트
/// </summary>
public class MailItemUi : MonoBehaviour
{
    [Header("Ui 컴포넌트들")]
    [SerializeField] private TextMeshPro titleText; //우편 제목
    [SerializeField] private TextMeshPro contentText; // 우편 내용
    [SerializeField] private TextMeshPro expireText; // 우편 만료시간
    [SerializeField] private Button claimButton; // 수령 버튼
    [SerializeField] private Image rewardIcon; // 첫 번째 대표 아이템 아이콘

    private string currnetMailId;
    public void Setup(mailItem mail)
    {
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

        //수령버튼 바인딩 (중복방지를 위해 한번 비우고 넣어줌)
        claimButton.onClick.RemoveAllListeners();
        claimButton.onClick.AddListener(OnClickClaim);
    }

    private async void OnClickClaim()
    {
        // 클릭 중복 방지 ( 서버 통신 중에는 비활성화)
        claimButton.interactable = false;

        bool success = await MailBoxManager.instance.ClaimMailAsync(currnetMailId);

        if(!success)
        {
            // 수령 실패 시 버튼 다시 활성화
            claimButton.interactable = true;
        }
        // 성공시 매니저의 이벤트(OnMailboxUpdated)가 나와서 리스트가 리프레시 되면서 자동으로 사라짐
    }



}
