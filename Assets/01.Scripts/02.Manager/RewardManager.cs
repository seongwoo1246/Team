using TMPro;
using UnityEngine;
using UnityEngine.UI;
//담당자 - 정성우
/*
 로그아웃 상태에서 있다가 게임에 들어왔을 때 방치형 보상을 보여주기 위해 만든 스크립트로 
실질적으로 주는 함수는 따로 있고 여기서는 흩어져 있는 보상 정보들을 한 군데 모아서 보여주기 위한 스크립트이다.
 */


/// <summary>
/// 방치형 보상 받기위한 매니저로 보상 관련 담당 예정
/// </summary>
public class RewardManager : Singleton<RewardManager>
{
    // 미접속 보상을 알려주기 위한 패널 .플레이어 경험치, 골드, 강화재료
    public GameObject RewardInfo;
    public TextMeshProUGUI GetPlayerExp;
    public TextMeshProUGUI GetPlayerReward;
    public TextMeshProUGUI GetUpgardMaterial;
    [SerializeField] private Button CloseRewardInfo;


    // null 체크만 추가
    // RewardInfo/CloseRewardInfo가 아직 Inspector에 연결 안 된 상태라 Start()가 계속 죽어서 널오류뜸..
    // 죽지만 않게 null 체크만 둘렀음
    private void Start()
    {
        if (RewardInfo != null)
        {
            RewardInfo.SetActive(true);
        }

        if (CloseRewardInfo != null)
        {
            CloseRewardInfo.onClick.AddListener(CloseInfo);
        }
    }


    public void CloseInfo()
    {
        if (RewardInfo != null)
        {
            RewardInfo.SetActive(false);
        }
    }

    


    /*
     보상 방식 = 매 시간 마다 바로 쌓이는 식으로 하고
    미접속 보상은 나간 시간 들어온 시간 계산해서 로그인했을 때 바로 보여주기

    빈 오브젝트에
    이미지 텍스트 버튼 집어넣어서 만들기.
     
     */





}
