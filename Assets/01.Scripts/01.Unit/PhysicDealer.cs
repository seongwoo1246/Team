/*
물리 딜러 (예: 전사).
가장 가까운 적 1체를 때리는 CharacterBase 기본 동작을 거의 그대로 씀
필요하면 아래 훅만 override 해서 연출 붙임!
*/

using UnityEngine;

/// <summary>
/// 근접 단일 대상 물리 공격 캐릭터
/// </summary>
public class PhysicDealer : CharacterBase
{
    [Header("물리 딜러 옵션")]
    [Tooltip("치명타가 터졌을 때만 재생할 강타 이펙트")]
    [SerializeField] private ParticleSystem heavyHitEffect;

    /// <summary>
    /// 물리 딜러의 공격. 기본은 부모의 "가장 가까운 적 1체 공격"을 그대로 사용
    /// 다른 방식이 필요하면 이 함수를 override
    /// </summary>
    protected override void PerformAttack()
    {
        // 단일 대상 물리 공격은 부모 구현으로 충분합니다
        base.PerformAttack();
    }

    /// <summary>
    /// 공격 직후 훅. 강타 이펙트가 지정돼 있으면 재생
    /// </summary>
    protected override void OnAfterAttack()
    {
        if (heavyHitEffect != null)
        {
            heavyHitEffect.Play();
        }
    }
}
