/* 담담자 - 송태훈
비동기 작업 대기 시 스피너 회전 연출과 함께 상태 진행 메시지를 표시하는 범용 로딩 팝업
 */
using Cysharp.Threading.Tasks;
using UnityEngine;
using UtilDebug = DebugLogger<LoadingStatusPopupUI>;

public class LoadingStatusPopupUI : MonoBehaviour
{
    [Header("UI 바인딩")]
    [SerializeField] private TMPro.TMP_Text statusText;
    [SerializeField] private RectTransform spinner; // 회전시킬 스피너 아이콘

    [Header("설정")]
    [SerializeField] private float defaultDisplayTime = 0.5f;  // 기본 최소 유지 시간

    private bool isRotating = false;
    private float showTime; // 팝업이 열린 실시간 타임 스탬프

    private void Update()
    {
        if (isRotating && spinner != null)
        {
            spinner.Rotate(0f, 0f, -360f * Time.unscaledDeltaTime);
        }
    }

    /// <summary>
    /// 로딩 팝업을 열고 메시지를 표시합니다.
    /// </summary>
    public void ShowLoading(string message)
    {
        UtilDebug.Log($"[LoadingPopup] {message}");
        if (statusText != null)
            statusText.text = message;

        showTime = Time.realtimeSinceStartup;   // 타임 스탬프 기록
        isRotating = true;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 상태 메시지만 갱신합니다.
    /// </summary>
    public void UpdateMessage(string message)
    {
        UtilDebug.Log($"[LoadingPopup] {message}");
        if (statusText != null)
            statusText.text = message;
    }

    /// <summary>
    /// 팝업이 활성화된 시점부터 계산하여 설정된 최소 노출 시간 동안 대기(Unscaled Time 기준)한 뒤 부드럽게 창을 닫습니다
    /// </summary>
    public async UniTask HideAsync(float minDisPlayTime = -1f)
    {
        if(!gameObject.activeSelf) return;

        float targetDuration = (minDisPlayTime >= 0f) ? minDisPlayTime : defaultDisplayTime;
        float elapsedTime = Time.realtimeSinceStartup - showTime;
        float remainingTime = targetDuration - elapsedTime;

        if(remainingTime > 0f)
        {
            await UniTask.Delay((int)(remainingTime * 1000), ignoreTimeScale: true);
        }

        isRotating = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 특정 완료 메시지나 에러 안내를 화면에 일정 시간 유지시킨 후 자동으로 숨깁니다 (실패/완료 알림용)
    /// </summary>
    public async UniTask ShowMessageAndHideAsync(string message, float delaySeconds = 1.5f)
    {
        UtilDebug.Log($"[LoadingPopup] {message}");
        isRotating = false;
        if (statusText != null)
            statusText.text = message;

        gameObject.SetActive(true);
        await UniTask.Delay((int)(delaySeconds * 1000), ignoreTimeScale: true);
        ForceHide();
    }

    public void ForceHide()
    {
        isRotating = false;
        gameObject.SetActive(false);
    }
}