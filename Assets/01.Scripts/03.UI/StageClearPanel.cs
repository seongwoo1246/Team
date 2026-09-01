/*
스테이지 클리어 시 뜨는 선택 화면. 다음 스테이지 / 파밍으로 버튼을 눌러 진행 방향을 고름
StageManager는 클리어해도 자동으로 아무 데도 안 가고 대기만 하므로, 이 패널이 그 대기 상태의 UI
*/

using UnityEngine;

/// <summary>
/// StageManager.StageCleared를 구독해서 클리어 선택 화면을 띄움
/// 버튼 2개(다음 스테이지 / 파밍으로)의 OnClick을 각각 OnClickNextStage / OnClickReturnToFarming에 연결해서 쓴다
/// </summary>
public sealed class StageClearPanel : MonoBehaviour
{
    [Tooltip("클리어 화면 전체 패널 (평소엔 꺼져있다가 클리어 순간에만 켜짐)")]
    [SerializeField] private GameObject panelRoot;

    // 방금 클리어한 스테이지 번호 (다음 스테이지 버튼 누를 때 씀)
    private int _clearedStageNumber;

    private void OnEnable()
    {
        if (StageManager.instance != null)
        {
            StageManager.instance.StageCleared += OnStageCleared;
            StageManager.instance.ChallengeStarted += OnChallengeStarted;
        }

        SetPanelActive(false);
    }

    private void OnDisable()
    {
        if (StageManager.instance != null)
        {
            StageManager.instance.StageCleared -= OnStageCleared;
            StageManager.instance.ChallengeStarted -= OnChallengeStarted;
        }
    }

    /// <summary>스테이지 클리어 순간 선택 패널을 띄운다</summary>
    private void OnStageCleared(int stageNumber)
    {
        _clearedStageNumber = stageNumber;
        SetPanelActive(true);
    }

    /// <summary>새 챌린지가 시작되면(다음 스테이지 버튼으로 진입한 경우 포함) 패널을 닫는다</summary>
    private void OnChallengeStarted(int stageNumber)
    {
        SetPanelActive(false);
    }

    /// <summary>"다음 스테이지" 버튼 OnClick에 연결</summary>
    public void OnClickNextStage()
    {
        SetPanelActive(false);

        if (StageManager.instance != null)
        {
            StageManager.instance.ContinueToNextStage(_clearedStageNumber);
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
