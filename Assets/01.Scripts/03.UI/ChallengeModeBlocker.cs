/*
챌린지 모드 중엔 골드 확인/강화를 못하게 막는 스크립트. blockerRoot(까만 이미지)를
챌린지 모드일 때만 켜서 그밑에 있는 Party_Panel(골드 텍스트 + 강화 카드들)을
시각적으로 가리는 동시에 클릭도(raycastTarget) 막는다
*/

using UnityEngine;

/// <summary>
/// StageManager.CurrentMode를 봐서 blockerRoot를 챌린지 모드일 때만 활성화한다
/// </summary>
public sealed class ChallengeModeBlocker : MonoBehaviour
{
    [Tooltip("챌린지 모드일 때 켤 가림막 오브젝트 (까만 Image, raycastTarget 켜져있어야 클릭도 막힘)")]
    [SerializeField] private GameObject blockerRoot;

    // OnEnable에서 캐싱해두고 그 뒤로는 이 캐시만씀 (씬종료 시 .instance 재호출로
    // Singleton<T>가 새 오브젝트를 만들어버리는 문제를 피하기 위함)
    private StageManager _stageManager;

    private void OnEnable()
    {
        _stageManager = StageManager.instance;
    }

    private void Update()
    {
        if (blockerRoot == null || _stageManager == null)
        {
            return;
        }

        bool isChallengeMode = _stageManager.CurrentMode == StageMode.Challenge;

        // 매프레임 무조건 실제모드에 맞춰줌
        blockerRoot.SetActive(isChallengeMode);
    }
}
