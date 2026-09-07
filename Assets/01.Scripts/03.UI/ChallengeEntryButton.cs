/*
챌린지 스테이지 진입 버튼용 스크립트. Button의 OnClick에 EnterChallenge()를 연결해서 씀

예전엔 인스펙터에 고정해둔 스테이지 번호로만 들어갔는데(항상 1스테이지),
지금까지 클리어한 최고 스테이지 다음(MaxClearedStage + 1)으로 들어가게 고침 -
안 그러면 스테이지1을 깨고 파밍 갔다가 다시 챌린지 입장을 눌러도 계속 1스테이지로만 감
*/

using UnityEngine;

/// <summary>
/// 지금까지 클리어한 최고 스테이지 다음 스테이지로 챌린지를 시작하는 버튼
/// Button의 OnClick에 EnterChallenge()를 연결
/// </summary>
public sealed class ChallengeEntryButton : MonoBehaviour
{
    /// <summary>
    /// 버튼 OnClick에 연결하는 함수. 클리어한 최고 스테이지 + 1로 챌린지를 시작
    /// (아직 하나도 안 깼으면 MaxClearedStage가 0이라 자동으로 1스테이지부터 시작됨)
    /// </summary>
    public void EnterChallenge()
    {
        if (StageManager.instance == null)
        {
            DebugLogger<ChallengeEntryButton>.LogWarning("StageManager 인스턴스를 찾을 수 없음");
            return;
        }

        StageManager.instance.EnterChallenge(StageManager.instance.MaxClearedStage + 1);
    }
}
