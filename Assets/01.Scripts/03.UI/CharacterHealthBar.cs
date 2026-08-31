/*
캐릭터 머리 위에 붙는 체력바. 챌린지 모드에서만 보이고(파밍은 체력 관리를 안 하니까)
CurrentHP/MaxHP 비율만큼 fillBar를 가로로 스케일해서 채워진 정도를 보여줌
*/

using UnityEngine;

/// <summary>
/// 캐릭터 위에 떠있는 체력바. 챌린지 모드에서만 보이고 CurrentHP 비율로 채워짐
/// </summary>
public sealed class CharacterHealthBar : MonoBehaviour
{
    [Tooltip("체력을 표시할 대상 캐릭터")]
    [SerializeField] private CharacterBase target;

    [Tooltip("배경 바 렌더러 (항상 꽉 찬 상태로 표시)")]
    [SerializeField] private SpriteRenderer backgroundRenderer;

    [Tooltip("체력 비율만큼 가로로 채워지는 바")]
    [SerializeField] private Transform fillBar;

    [Tooltip("채워지는 바의 렌더러")]
    [SerializeField] private SpriteRenderer fillRenderer;

    // fillBar가 꽉찬상태 일때의 가로 스케일. Awake 시점 값을 기준으로 삼아서
    // Update에서 비율만큼 줄임 (그냥 ratio를 대입하면 원래 너비를 잃어버림)
    private float _fullFillScaleX = 1f;

    private void Awake()
    {
        if (fillBar != null)
        {
            _fullFillScaleX = fillBar.localScale.x;
        }
    }

    private void Update()
    {
        // 스크립트가 붙은 오브젝트 자체는 항상 활성 상태로 두고, 렌더러만 켜고 끈다
        // (SetActive(false)로 자기 자신을 끄면 Update가 멈춰서 다시 챌린지에 들어가도 안 켜짐)
        bool isChallengeMode = StageManager.instance != null && StageManager.instance.CurrentMode == StageMode.Challenge;

        if (backgroundRenderer != null)
        {
            backgroundRenderer.enabled = isChallengeMode;
        }

        if (fillRenderer != null)
        {
            fillRenderer.enabled = isChallengeMode;
        }

        if (!isChallengeMode || target == null || fillBar == null)
        {
            return;
        }

        float ratio = target.MaxHP > 0f ? Mathf.Clamp01(target.CurrentHP / target.MaxHP) : 0f;
        Vector3 scale = fillBar.localScale;
        scale.x = _fullFillScaleX * ratio;
        fillBar.localScale = scale;
    }
}
