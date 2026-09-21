// 작성자: 김주연
/*
파밍 ↔ 챌린지 모드가 바뀔 때, 검은 화면이 한쪽에서 들어와 화면을 가렸다가 반대쪽으로
빠져나가면서 자연스럽게 전환되는 와이프연출
*/

using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

/// <summary>
/// StageManager.ModeChanged를 구독해서 검은 패널을 옆으로 슬라이드시키는 와이프 전환 연출
/// </summary>
public sealed class ModeTransitionWipe : MonoBehaviour, ILoadable
{
    [Header("연결")]
    [Tooltip("화면을 가리는 검은 이미지의 RectTransform (풀스크린 크기로 세팅되어 있어야 함)")]
    [SerializeField] private RectTransform wipePanel;

    [Header("연출 설정")]
    [Tooltip("왼쪽 화면 밖에서 오른쪽 화면 밖까지 슬라이드하는 데 걸리는 총 시간(초)")]
    [SerializeField] private float slideDuration = 0.5f;

    private StageManager _stageManager;
    private CancellationTokenSource _playCts;

    public int LoadOrder => 40;

    private void Awake()
    {
        SceneLoadManager.Instance.RegisterLoadable(this);
        ParkOffScreen();
    }

    /// <summary>평소엔 왼쪽 화면 밖에 세워둬서 안 보이게 한다</summary>
    private void ParkOffScreen()
    {
        if (wipePanel == null)
        {
            return;
        }

        float screenWidth = wipePanel.rect.width;
        wipePanel.anchoredPosition = new Vector2(-screenWidth, 0f);
    }

    private void OnEnable()
    {
        TryBindStageManager();
    }

    private void OnDisable()
    {
        UnbindStageManager();
    }

    public UniTask OnSceneLoadCreate(SceneId scene) => UniTask.CompletedTask;

    public void Init(SceneId scene)
    {
        if (scene != SceneId.LobbySceneTest) return;
        TryBindStageManager();
    }

    public void OnSceneDestory(SceneId scene)
    {
        UnbindStageManager();
        _playCts?.Cancel();
    }

    private void TryBindStageManager()
    {
        if (ServiceLocator.TryGet<StageManager>(out StageManager stageMng))
        {
            _stageManager = stageMng;
            _stageManager.ModeChanged -= OnModeChanged;
            _stageManager.ModeChanged += OnModeChanged;
        }
    }

    private void UnbindStageManager()
    {
        if (_stageManager != null)
        {
            _stageManager.ModeChanged -= OnModeChanged;
        }
    }

    /// <summary>모드가 바뀔 때마다(파밍 ↔ 챌린지) 와이프 연출을 새로 재생</summary>
    /// <param name="mode">바뀐 후의 모드 (여기선 안 씀 - 방향은 항상 동일)</param>
    private void OnModeChanged(StageMode mode)
    {
        PlayWipeAsync().Forget();
    }

    /// <summary>
    /// 왼쪽 화면 밖 -> 화면 중앙(전체 커버) -> 오른쪽 화면 밖 순서로 한 번에 슬라이드시킨다
    /// </summary>
    private async UniTaskVoid PlayWipeAsync()
    {
        if (wipePanel == null)
        {
            return;
        }

        // 모드가 연달아 바뀌면 이전 연출은 취소하고 새로 시작 (꼬이지 않게)
        _playCts?.Cancel();
        _playCts = new CancellationTokenSource();
        CancellationToken token = _playCts.Token;

        float screenWidth = wipePanel.rect.width;
        Vector2 startPos = new Vector2(-screenWidth, 0f);
        Vector2 endPos = new Vector2(screenWidth, 0f);

        wipePanel.anchoredPosition = startPos;

        float elapsed = 0f;
        try
        {
            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float ratio = Mathf.Clamp01(elapsed / slideDuration);
                wipePanel.anchoredPosition = Vector2.Lerp(startPos, endPos, ratio);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        wipePanel.anchoredPosition = endPos;
    }
}
