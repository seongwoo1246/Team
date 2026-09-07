using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;




/// <summary>
/// 지금은 아니지만 나중에 광고를 넣어야 하게 될 때 필요한 클래스(이번 프로젝트에서는 짧은 아무 동영상으로 대체함)
/// </summary>
public class ADManager : MonoBehaviour
{
    [Header("광고(x) UI들")]
    [SerializeField]private Button AdViewBtn;
    [SerializeField]private Button closeAdBtn;
    [SerializeField] private TextMeshProUGUI AdSkipTimeText;

    [Header("광고(x) 영상들")]
    public VideoClip RewardAD;
    public VideoClip RewardAD1;
    public VideoClip InterstitialAD;
    public VideoClip InterstitialAD1;

    // 상시 구석에서 있는 광고
    public Image BannerAD;
    public Image BannerAD1;

    //영상 스킵까지의 시간
    private int AdSkipTime = Random.Range(5, 15);

    // 보면 보상을 줄 영상
    [SerializeField] private List<VideoClip> RewardADList = new List<VideoClip>();
    // 중간에 강제로 나올 영상
    [SerializeField] private List<VideoClip> InterstitialADList = new List<VideoClip>();


    private void Awake()
    {
        
    }



    public void OpenRewardAD()
    {
        
    }

    public void CloseRewardAD()
    {

    }


}
