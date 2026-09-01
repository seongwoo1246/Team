/*
몬스터 머리 위에 붙는 체력바. CharacterHealthBar와 구조는 동일하고, 대상만 다름
몬스터는 파밍/챌린지 상관없이 항상 실제 체력이 깎이므로(캐릭터와 달리 챌린지 모드 한정 표시가 아님)
모드 구분 없이 항상 보여주게 했음
*/

using UnityEngine;

/// <summary>
/// 몬스터 위에 떠있는 체력바. CurrentHP 비율로 채워지고 모드 상관없이 항상 표시됨
/// </summary>
public sealed class MonsterHealthBar : MonoBehaviour
{
    [Tooltip("체력을 표시할 대상 몬스터 (보통 이 오브젝트의 부모)")]
    [SerializeField] private Monster target;

    [Tooltip("배경 바 렌더러 (항상 꽉 찬 상태로 표시)")]
    [SerializeField] private SpriteRenderer backgroundRenderer;

    [Tooltip("체력 비율만큼 가로로 채워지는 바")]
    [SerializeField] private Transform fillBar;

    [Tooltip("채워지는 바의 렌더러")]
    [SerializeField] private SpriteRenderer fillRenderer;

    // fillBar가 꽉찬상태 일때의 가로 스케일. Awake 시점 값을 기준으로 삼아서
    // Update에서 비율만큼 줄임 (주의!그냥 ratio를 대입하면 원래 너비를 잃어버림)
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
        if (target == null || fillBar == null)
        {
            return;
        }

        float ratio = target.MaxHP > 0f ? Mathf.Clamp01(target.CurrentHP / target.MaxHP) : 0f;
        Vector3 scale = fillBar.localScale;
        scale.x = _fullFillScaleX * ratio;
        fillBar.localScale = scale;
    }
}
