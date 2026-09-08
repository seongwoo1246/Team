using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Cysharp.Threading.Tasks;
using System.Threading;
using Debug = DebugLogger<ADManager>;
using System;
using System.Threading.Tasks;



/// <summary>
/// 지금은 아니지만 나중에 광고를 넣어야 하게 될 때 필요한 클래스(이번 프로젝트에서는 짧은 아무 동영상으로 대체함)
/// </summary>
public class ADManager : Singleton<ADManager>
{
    [Header("광고(x) UI들")]
    [SerializeField] private GameObject adPanel; // 광고 패널
    [SerializeField]private Button closeAdBtn; // 광고 스킵 및 닫기 버튼
    [SerializeField]private Button openAdBtn; // 광고 열기 버튼
    [SerializeField] private TextMeshProUGUI AdSkipTimeText; // 광고 남은 시간 텍스트

    [Header("비디오 플레이어")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("광고 데이터 풀")]
    // 보면 보상을 줄 영상
    [SerializeField] private List<VideoClip> RewardADList = new List<VideoClip>();
    // 중간에 강제로 나올 영상
    [SerializeField] private List<VideoClip> InterstitialADList = new List<VideoClip>();

    //씬 파괴 및 광고 취소 제어용 
    private CancellationTokenSource adCancellationTokenSource;


    protected override void Awake()
    {
        base.Awake();

        if(adPanel != null ) adPanel.SetActive(false);
        if (closeAdBtn != null) closeAdBtn.onClick.AddListener(CloseRewardAD);
        if (openAdBtn != null) openAdBtn.onClick.AddListener(OnClickAdRewardButton);
        if (AdSkipTimeText != null) AdSkipTimeText.gameObject.SetActive(false);
       
    }

    private void OnDestroy()
    {
        //메모리 누수방지용 토큰취소
        adCancellationTokenSource? .Cancel();
        adCancellationTokenSource?.Dispose();
    }

    #region 보상형 광고 (ShowRewardADAsync)


    /// <summary>
    /// 보상형 광고 재생 비동기 함수
    /// </summary>
    /// <returns>시청완료(보상지급)</returns>
    public async UniTask<bool> ShowRewardADAsync()
    {
        if(RewardADList.Count == 0)
        {
            Debug.LogWarning("보상형 광고 리스트가 비워져 있습니다.");
            return false;
        }

        // 이전 비동기 작업 취소 및 새 토큰 생성
        adCancellationTokenSource?.Cancel();
        adCancellationTokenSource = new CancellationTokenSource();
        var token = adCancellationTokenSource.Token;

        VideoClip selectedClip = RewardADList[UnityEngine.Random.Range(0, RewardADList.Count)];
        int skipDuratuon = UnityEngine.Random.Range(10, 15);

        //UI 활성화 및 영상 재생
        SetupAdUi(selectedClip);
        openAdBtn.gameObject.SetActive(false);

        try
        {
            // 스킵 시간이 다 될 때 까지 타이머 실행
            bool isCompleted = await RunAdTimeerAsync(skipDuratuon,token);

            // 영상을 다 보든 닫기 버튼을 누르기 둘 중 빠른 쪽으로 해결
            var videoEndTcs = new UniTaskCompletionSource();

            VideoPlayer.EventHandler endHandler = (vp) => videoEndTcs.TrySetResult();
            videoPlayer.loopPointReached += endHandler;

            try
            {
                // UniTask.WhenAny ) 둘 중 하나라도 먼저 끝나면 다음으로 진행
                // Task 0  닫기 버튼 클릭 대기
                // Task 1  영상을 끝까지 보기
                await UniTask.WhenAny(closeAdBtn.OnClickAsync(token),
                    // 취소 기능이 없는 비동기에게 중간에 취소하기 위해 붙는 것 => AttachExternalCancellation(token));
                    videoEndTcs.Task.AttachExternalCancellation(token));
            }
            finally
            {
                videoPlayer.clip = null;
                videoPlayer.loopPointReached -= endHandler;
            }

            return isCompleted;
        }
        catch (OperationCanceledException)
        {
            // 중간에 광고가 끊어졌을 때
            Debug.Log("광고 시청이 끊어졌습니다. ");
            return false;
        }
        finally
        {
            CloseRewardAD();
        }
    }

    #endregion

    #region 전면 광고 (ShowInterstitialADAsync)
    /// <summary>
    /// 전면 광고로 게임 중간에 강제로 나오는 식의 광고 
    /// 사용방법은 async void 함수 중간에 await ADManager.instance.ShowInterstitialADAsync(); 식으로 사용하면 광고가 나옴;
    /// </summary>
    /// <returns></returns>
    public async UniTask ShowInterstitialADAsync()
    {
        if (InterstitialADList.Count == 0) return;

        adCancellationTokenSource?.Cancel();
        adCancellationTokenSource = new CancellationTokenSource();
        var token = adCancellationTokenSource.Token;

        int skipDuratuon = UnityEngine.Random.Range(10, 15);

        VideoClip selectedClip = InterstitialADList[UnityEngine.Random.Range(0, InterstitialADList.Count)];

        SetupAdUi( selectedClip);

        // 영상을 다 보든 닫기 버튼을 누르기 둘 중 빠른 쪽으로 해결
        var videoEndTcs = new UniTaskCompletionSource();

        VideoPlayer.EventHandler endHandler = (vp) => videoEndTcs.TrySetResult();
        videoPlayer.loopPointReached += endHandler;

        try
        {
            await RunAdTimeerAsync(skipDuratuon, token);

                await UniTask.WhenAny(closeAdBtn.OnClickAsync(token),
                    videoEndTcs.Task.AttachExternalCancellation(token));
            
            
              
            

        }
        catch (OperationCanceledException)
        {
            Debug.Log("광고가 중단 되었습니다.");
        }
        finally
        {
            videoPlayer.clip = null;
            videoPlayer.loopPointReached -= endHandler;
            CloseRewardAD();
        }

    }

    #endregion

    #region 기본 세팅및 Ui관련

    private void SetupAdUi(VideoClip clip)
    {
        // 미리 다음 영상을 로드 하고 활성화 해주기
        videoPlayer.Prepare();

        adPanel.SetActive(true);
        closeAdBtn.gameObject.SetActive(false); // 타이머가 끝나기 전까지 버튼 비활성화

       videoPlayer.clip = clip;
        videoPlayer.Play();
    }

    /// <summary>
    /// 1초씩 카운트 다운 하기 위한 함수
    /// </summary>
    /// <param name="duration"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async UniTask<bool> RunAdTimeerAsync(int duration, CancellationToken token)
    {
        int remainingTime = duration;
        AdSkipTimeText.gameObject.SetActive(true);
        while (remainingTime > 0)
        {
            if(AdSkipTimeText != null)
            {
                AdSkipTimeText.text = $"{remainingTime}초 후 스킵 가능^^";
            }
            //1초 동안 비동기 대기 (타임스케쥴과는 무관)
            await UniTask.Delay(TimeSpan.FromSeconds(1),ignoreTimeScale:true,cancellationToken : token);
            remainingTime--;
        }
        //스킵 시간후 종료 버튼 활성화
        if (AdSkipTimeText != null) AdSkipTimeText.text = "스킵 가능";
       closeAdBtn.gameObject.SetActive(true);

        return true;
    }

    public void CloseRewardAD()
    {
        openAdBtn.gameObject.SetActive(true);

        if(videoPlayer != null&&videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
           
        }
        if(adPanel != null)
        {
            adPanel.SetActive(false);
        }
        if(AdSkipTimeText!= null)
        {
            AdSkipTimeText.gameObject.SetActive(false);
        }
    }


    public async void OnClickAdRewardButton()
    {
        bool isRewarded = await ShowRewardADAsync();
        if(isRewarded)
        {
            Debug.Log("여기서 보상 지급 해주면 됨");
        }
        else
        {
            Debug.Log("광고가 취소됨 ");
        }
    }


    #endregion
}
