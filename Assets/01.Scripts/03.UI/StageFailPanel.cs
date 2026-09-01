/*
스테이지 실패 시 뜨는 선택 화면. 다시 하기 / 파밍으로 버튼을 눌러 진행 방향을 고름
StageManager는 실패해도 클리어와 마찬가지로 자동으로 아무 데도 안 가고 대기만 하므로,
이 패널이 그 대기 상태의 UI
*/

using UnityEngine;

/// <summary>
/// StageManager.StageFailed를 구독해서 실패 선택 화면을 띄움
/// 버튼 2개(다시 하기 / 파밍으로)의 OnClick을 각각 OnClickRetry / OnClickReturnToFarming에 연결해서 쓴다
/// </summary>
public sealed class StageFailPanel : MonoBehaviour
{
    [Tooltip("실패 화면 전체 패널 (평소엔 꺼져있다가 실패 순간에만 켜짐)")]
    [SerializeField] private GameObject panelRoot;

    // 방금 실패한 스테이지 번호 (다시 하기 버튼 누를 때 씀)
    private int _failedStageNumber;

    private void OnEnable()
    {
        if (StageManager.instance != null)
        {
            StageManager.instance.StageFailed += OnStageFailed;
            StageManager.instance.ChallengeStarted += OnChallengeStarted;
        }

        SetPanelActive(false);
    }

    private void OnDisable()
    {
        if (StageManager.instance != null)
        {
            StageManager.instance.StageFailed -= OnStageFailed;
            StageManager.instance.ChallengeStarted -= OnChallengeStarted;
        }
    }

    /// <summary>스테이지 실패 순간 선택 패널을 띄운다</summary>
    private void OnStageFailed(int stageNumber)
    {
        _failedStageNumber = stageNumber;
        SetPanelActive(true);
    }

    /// <summary>새 챌린지가 시작되면(다시 하기 버튼으로 진입한 경우 포함) 패널을 닫는다</summary>
    private void OnChallengeStarted(int stageNumber)
    {
        SetPanelActive(false);
    }

    /// <summary>"다시 하기" 버튼 OnClick에 연결</summary>
    public void OnClickRetry()
    {
        SetPanelActive(false);

        if (StageManager.instance != null)
        {
            StageManager.instance.RetryStage(_failedStageNumber);
        }
    }

    /// <summary>"파밍으로" 버튼 OnClick에 연결</summary>
    public void OnClickReturnToFarming()
    {
        SetPanelActive(false);

        if (StageManager.instance != null)
        {
            StageManager.instance.EnterFarming();
        }
    }

    private void SetPanelActive(bool active)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(active);
        }
    }
}
