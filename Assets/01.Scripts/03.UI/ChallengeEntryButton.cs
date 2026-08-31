/*
챌린지 스테이지 진입 버튼용 스크립트. Button의 OnClick에 EnterChallenge()를 연결해서 씀
*/

using UnityEngine;

/// <summary>
/// 지정한 스테이지 번호로 챌린지를 시작하는 버튼. Button의 OnClick에 EnterChallenge()를 연결
/// </summary>
public sealed class ChallengeEntryButton : MonoBehaviour
{
    [Tooltip("이 버튼을 누르면 진입할 스테이지 번호")]
    [SerializeField] private int stageNumber = 1;

    /// <summary>
    /// 버튼 OnClick에 연결하는 함수. 지정된 스테이지 번호로 챌린지를 시작
    /// </summary>
    public void EnterChallenge()
    {
        if (StageManager.instance == null)
        {
            DebugLogger<ChallengeEntryButton>.LogWarning("StageManager 인스턴스를 찾을 수 없음");
            return;
        }

        StageManager.instance.EnterChallenge(stageNumber);
    }
}
