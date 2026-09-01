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

    [Header("스킬1: 강타")]
    [Tooltip("강타 데미지 배율 (평타 대비)")]
    [SerializeField] private float powerStrikeMultiplier = 2.5f;

    [Header("스킬2: 휩쓸기")]
    [Tooltip("휩쓸기 데미지 배율 (평타 대비, 사거리 안 적 전체에게 각각 적용)")]
    [SerializeField] private float cleaveMultiplier = 1f;

    // 휩쓸기용 OverlapCircle 결과 재사용 버퍼
    private readonly Collider2D[] _cleaveBuffer = new Collider2D[MAX_TARGET_BUFFER];

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

    /// <summary>스킬1: 강타. 가장 가까운 적 1체에게 평타보다 훨씬 센 단일 타격</summary>
    protected override void UseSkill1()
    {
        IEntity target = GetNearestEntity(EnemyLayer);
        if (target == null || target.IsDead)
        {
            return;
        }

        target.TakeDamage(Power * powerStrikeMultiplier);
    }

    /// <summary>스킬2: 휩쓸기. 사거리 안 적 전체에게 동시에 피해</summary>
    protected override void UseSkill2()
    {
        int count = FindEntitiesInRange(EnemyLayer, _cleaveBuffer);
        if (count <= 0)
        {
            return;
        }

        float damage = Power * cleaveMultiplier;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _cleaveBuffer[i];
            if (hit == null)
            {
                continue;
            }

            if (!hit.TryGetComponent(out IEntity target) || target.IsDead)
            {
                continue;
            }

            target.TakeDamage(damage);
        }
    }

    /// <summary>강타/휩쓸기 공용: 사거리 안에 적이 있을 때만 사용 가능
    /// (오토 스킬이 근처에 몬스터가 없을 때 헛발질로 쿨다운만 날리지 않게 막는다)</summary>
    protected override bool CanUseSkill1() => HasEnemyInRange();

    /// <summary>스킬2(휩쓸기)도 동일한 조건</summary>
    protected override bool CanUseSkill2() => HasEnemyInRange();

    private bool HasEnemyInRange() => GetNearestEntity(EnemyLayer) != null;
}
