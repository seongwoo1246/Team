/*
마법 딜러 (예: 메이지). 다수 공격.
2D 원형 범위(OverlapCircle)로 사거리 안 적을 모두 찾아, 최대 대상 수만큼 동시에 피해를 준다.
*/

using UnityEngine;

/// <summary>
/// 범위 안 여러 적을 동시에 공격하는 마법 캐릭터.
/// </summary>
public class MagicDealer : CharacterBase
{
    [Header("마법 딜러 옵션")]
    [Tooltip("한 번에 맞출 수 있는 최대 적 수(없어도 되긴한데 일단넣어봄)")]
    [SerializeField] private int maxTargets = 5;

    [Tooltip("범위 피해 배율. 1.0이면 단일 공격력과 동일, 0.7이면 70%")]
    [Range(0.1f, 1f)]
    [SerializeField] private float areaDamageMultiplier = 0.7f;

    [Tooltip("폭발 이펙트")]
    [SerializeField] private ParticleSystem explosionEffect;

    [Header("스킬1: 파이어볼")]
    [Tooltip("파이어볼 데미지 배율 (평타 대비). 평소와 반대로 한 대상에게 크게 터짐")]
    [SerializeField] private float fireballMultiplier = 3f;

    [Header("스킬2: 메테오")]
    [Tooltip("메테오 데미지 배율 (평타 대비). maxTargets 제한 없이 사거리 안 전체에게 적용")]
    [SerializeField] private float meteorMultiplier = 1.2f;

    // OverlapCircle 결과 재사용 버퍼 (매 공격마다 새로 만들지 않는다)
    private readonly Collider2D[] _hitBuffer = new Collider2D[MAX_TARGET_BUFFER];

    // 메테오(스킬2)용 OverlapCircle 결과 재사용 버퍼. 평타용(_hitBuffer)이랑 겹쳐 쓰지 않게 따로 둠
    private readonly Collider2D[] _skillBuffer = new Collider2D[MAX_TARGET_BUFFER];

    /// <summary>
    /// 마법 공격. 사거리 안 적을 모두 찾아 maxTargets 만큼 범위 피해를줌
    /// </summary>
    protected override void PerformAttack()
    {
        // 이름만 Find지 Unity의 Find 계열 API를 안씀! 걱정마세요 (Physics2D.OverlapCircle) 이런거임
        int count = FindEntitiesInRange(EnemyLayer, _hitBuffer);
        if (count <= 0)
        {
            return;
        }

        float damage = Power * areaDamageMultiplier;
        int hitCount = 0;

        for (int i = 0; i < count; i++)
        {
            if (hitCount >= maxTargets)
            {
                break;
            }

            Collider2D hit = _hitBuffer[i];
            if (hit == null)
            {
                continue;
            }

            if (!hit.TryGetComponent(out IEntity target) || target.IsDead)
            {
                continue;
            }

            target.TakeDamage(damage);
            hitCount++;
        }
    }

    // 공격 직전 훅. 폭발 이펙트가 지정돼 있으면 재생
    protected override void OnBeforeAttack()
    {
        if (explosionEffect != null)
        {
            explosionEffect.Play();
        }
    }

    /// <summary>스킬1: 파이어볼. 가장 가까운 적 1체에게 평타보다 훨씬 센 단일 타격</summary>
    protected override void UseSkill1()
    {
        IEntity target = GetNearestEntity(EnemyLayer);
        if (target == null || target.IsDead)
        {
            return;
        }

        target.TakeDamage(Power * fireballMultiplier);
    }

    /// <summary>스킬2: 메테오. maxTargets 제한 없이 사거리 안 적 전체에게 피해</summary>
    protected override void UseSkill2()
    {
        int count = FindEntitiesInRange(EnemyLayer, _skillBuffer);
        if (count <= 0)
        {
            return;
        }

        float damage = Power * meteorMultiplier;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _skillBuffer[i];
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

    /// <summary>파이어볼/메테오 공용: 사거리 안에 적이 있을 때만 사용 가능
    /// (오토 스킬이 근처에 몬스터가 없을 때 헛발질로 쿨다운만 날리지 않게 막는다)</summary>
    protected override bool CanUseSkill1() => HasEnemyInRange();

    /// <summary>스킬2(메테오)도 동일한 조건</summary>
    protected override bool CanUseSkill2() => HasEnemyInRange();

    private bool HasEnemyInRange() => GetNearestEntity(EnemyLayer) != null;
}
