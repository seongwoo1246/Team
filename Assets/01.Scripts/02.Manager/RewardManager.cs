using TMPro;
using UnityEngine;
using UnityEngine.UI;


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
    private Button CloseRewardInfo;


    private void Start()
    {
        RewardInfo.SetActive(true);

        CloseRewardInfo.onClick.AddListener(CloseInfo);
    }


    public void CloseInfo()
    {
        RewardInfo.SetActive(false);
    }




    /*
     보상 방식 = 매 시간 마다 바로 쌓이는 식으로 하고
    미접속 보상은 나간 시간 들어온 시간 계산해서 로그인했을 때 바로 보여주기

    빈 오브젝트에
    이미지 텍스트 버튼 집어넣어서 만들기.
     
     */





}
