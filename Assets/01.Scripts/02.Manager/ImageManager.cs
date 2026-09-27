using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class ImageManager : MonoBehaviour
{



    [Header("배경")]
    [SerializeField] Image BattleImage;
    [SerializeField] Image BattleImage1;
    [SerializeField] Image BattleImage2;

    [Header("dim")]
    [SerializeField] Image dim;

    [Header("배경 스크롤 설정")]
    [SerializeField] private float moveSpeed = 500f;
    [SerializeField] private float resetPosition = -1080f;
    [SerializeField] private float startPosition = 1080f;


    public CancellationTokenSource MoveCts;
    public bool isMoving = false;

    private void Awake()
    {
        if (dim != null)
        {

            Color color = dim.color;
            color.a = 0f;
            dim.color = color;

        }
    }

    #region 페이드 연출

    private async UniTask FadeOutAsync(float duration = 0.5f, CancellationToken cancellation = default)
    {
        if (dim == null) return;

        dim.gameObject.SetActive(true);

        float elapsedTime = 0f;
        Color color = dim.color;

        while (elapsedTime < duration)
        {
            cancellation.ThrowIfCancellationRequested();

            elapsedTime += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsedTime / duration);
            dim.color = color;

            await UniTask.Yield(PlayerLoopTiming.Update, cancellation);

        }
        color.a = 1f;
        dim.color = color;

    }
    private async UniTask FadeInAsync(float duration = 0.5f, CancellationToken cancellation = default)
    {
        if (dim == null) return;

        float elapsedTime = 0f;
        Color color = dim.color;



        while (elapsedTime < duration)
        {
            cancellation.ThrowIfCancellationRequested();

            elapsedTime += Time.deltaTime;
            color.a = Mathf.Clamp01(1f - (elapsedTime / duration));
            dim.color = color;

            await UniTask.Yield(PlayerLoopTiming.Update, cancellation);

        }

        color.a = 0f;
        dim.color = color;
        dim.gameObject.SetActive(false);
    }

    
    /// <summary>
    /// 화면 이동시 검정색으로 변했다가 밝아지는 연출
    /// </summary>
    /// <param name="duration"></param>
    /// <param name="holdDuration"></param>
    /// <returns></returns>
    public async UniTask FadeDim(float duration =0.5f,float holdDuration = 0.2f)
    {
        CancellationToken cancellation = this.GetCancellationTokenOnDestroy();

        await FadeOutAsync(duration, cancellation);

        await UniTask.Delay((int)(holdDuration *1000), cancellationToken :  cancellation);

        await FadeInAsync(duration, cancellation);
    }


    #endregion


    #region 배경 스크롤 이동 및 조건 정지


    public async UniTask MoveBackGround(System.Func<bool> stopCondition)
    {
        // 이미 이동 중이라면 기존 작업 취소 후 재시작
        StopMoveBackGround();


        MoveCts = new CancellationTokenSource();
        CancellationToken token = CancellationTokenSource.CreateLinkedTokenSource(MoveCts.Token,this.GetCancellationTokenOnDestroy()).Token;
        isMoving = true;

        try
        {
            while(isMoving)
            {
                //외부 조건이 true면 이동 중지
                if(stopCondition != null && stopCondition.Invoke())
                {
                    break;
                }

                ScrollImage(BattleImage);
                ScrollImage(BattleImage1);
                ScrollImage(BattleImage2);

                await UniTask.Yield(PlayerLoopTiming.Update, token);

            }
        }
        catch(System.OperationCanceledException)
        {
            return;
        }
        finally
        {
            isMoving = false;
        }

    }

    /// <summary>
    /// 화면이 스크롤 되는 함수
    /// </summary>
    /// <param name="image"></param>
    private void ScrollImage(Image image)
    {
        if (image == null) return;

        RectTransform rect = image.rectTransform;
        rect.anchoredPosition += Vector2.left * moveSpeed * Time.deltaTime;

        // 왼쪽 화면 밖으로 완전히 벗어나면 오른쪽으로 이동
        if(rect.anchoredPosition.x < resetPosition)
        {
            rect.anchoredPosition = new Vector2(startPosition,rect.anchoredPosition.y);
        }
        
    }

    /// <summary>
    /// 수동으로 스크롤을 멈춰야 할 때 호출
    /// </summary>
    public void StopMoveBackGround()
    {
        if(MoveCts != null)
        {
            MoveCts.Cancel();
            MoveCts.Dispose();
            MoveCts = null;
        }

        isMoving = false;
    }

    private void OnDestroy()
    {
        StopMoveBackGround();
    }

    #endregion


    public void BattleImageView()
    {
        BattleImage.gameObject.SetActive(true);
        BattleImage1.gameObject.SetActive(true);
        BattleImage2.gameObject.SetActive(true);
    }
    public void BattleImageNoView()
    {
        BattleImage.gameObject.SetActive(false);
        BattleImage1.gameObject.SetActive(false);
        BattleImage2.gameObject.SetActive(false);
    }



}


















