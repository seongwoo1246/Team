using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
// 담당장 정성우
/*
배틀 들어갈 때 배경을 담당하고 화면 전환 효과를 담당하고 있다.
배경을 무한 스크롤 형태를 이용해서 만들었지만 무슨 이유인지 배경이 멈춰서 움직이질 않는다. 
아무래도 스탑 위치가 잘 못 된게 아닐까 추측 중이다.

 */



public class ImageManager : MonoBehaviour
{
    [Header("배경")]
    [SerializeField] GameObject BattleImage;
    [SerializeField] GameObject BattleImage1;
    [SerializeField] GameObject BattleImage2;

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
            dim.gameObject.SetActive(false);
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

                ScrollObject(BattleImage);
                ScrollObject(BattleImage1);
                ScrollObject(BattleImage2);

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
    /// <param name="targetGo"></param>
    private void ScrollObject(GameObject targetGo)
    {
        if (targetGo == null) return;

        Transform tr = targetGo.transform;
        Vector2 pos = tr.localPosition;

        pos.x -= moveSpeed * Time.deltaTime;

        if(pos.x < resetPosition)
        {
            pos.x = startPosition;
        }

        tr.localPosition = pos;
        
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
        if (BattleImage != null) BattleImage.SetActive(true);
        if (BattleImage1 != null) BattleImage1.SetActive(true);
        if (BattleImage2 != null) BattleImage2.SetActive(true);
    }

    public void BattleImageNoView()
    {
        if (BattleImage != null) BattleImage.SetActive(false);
        if (BattleImage1 != null) BattleImage1.SetActive(false);
        if (BattleImage2 != null) BattleImage2.SetActive(false);
    }
}