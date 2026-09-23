// 작성자: 김주연
/*
궁수 - 원거리 다단히트형 물리 딜러. 치명타율이 높고 체력은 낮은 대신, 스킬로 여러 대상/여러 발을 때림
*/

using UnityEngine;

/// <summary>
/// 원거리 딜러 캐릭터. 스킬1은 관통 사격(사거리 안 여러 적 동시 타격), 스킬2는 단일 대상 연속 사격
/// </summary>
public class ArcherDealer : CharacterBase
{
    [Header("화살 비주얼")]
    [Tooltip("평타를 쏠 때 날아가는 화살 프리팹 (비주얼 전용, 데미지는 즉시 따로 적용됨)")]
    [SerializeField] private Arrow arrowPrefab;

    [Header("스킬1: 관통 사격")]
    [Tooltip("관통 사격 데미지 배율 (평타 대비, 맞는 대상마다 각각 적용됨)")]
    [SerializeField] private float piercingShotMultiplier = 1.2f;

    [Tooltip("관통 사격이 한 번에 맞출 수 있는 최대 대상 수")]
    [SerializeField] private int piercingShotMaxTargets = 3;

    [Header("스킬2: 속사")]
    [Tooltip("속사 1발당 데미지 배율 (평타 대비)")]
    [SerializeField] private float rapidShotMultiplier = 0.8f;

    [Tooltip("속사 발사 횟수")]
    [SerializeField] private int rapidShotCount = 4;

    // 관통 사격용 OverlapCircle 결과 재사용 버퍼
    private readonly Collider2D[] _piercingBuffer = new Collider2D[MAX_TARGET_BUFFER];

    // 화살 풀 예열 개수
    private const int ARROW_POOL_SIZE = 10;

    private static bool _arrowPoolRegistered = false;

    protected override void Awake()
    {
        base.Awake();

        if (!_arrowPoolRegistered && arrowPrefab != null)
        {
            ObjectPoolManagerTest.Instance.RegisterPool<Arrow>(Arrow.PoolKey, arrowPrefab.gameObject, ARROW_POOL_SIZE);
            _arrowPoolRegistered = true;
        }
    }

    protected override void PerformAttack()
    {
        IEntity target = GetLowestHpEntity(EnemyLayer);
        if (target == null || target.IsDead)
        {
            return;
        }

        FireArrowAt(target);
        DealDamage(target);
    }

    private void FireArrowAt(IEntity target)
    {
        if (arrowPrefab == null || target is not Component targetComponent)
        {
            return;
        }

        Arrow arrow = ObjectPoolManagerTest.Instance.Spawn<Arrow>(Arrow.PoolKey);
        if (arrow == null)
        {
            return;
        }

        arrow.transform.SetPositionAndRotation(transform.position, Quaternion.identity);
        arrow.Fire(targetComponent.transform.position);
    }

    /// <summary>
    /// 관통 사격. 사거리 안 적 중 가까운 순서로 최대 piercingShotMaxTargets명에게 각각 피해를 준다
    /// </summary>
    protected override void UseSkill1()
    {
        int count = FindEntitiesInRange(EnemyLayer, _piercingBuffer);
        if (count <= 0)
        {
            return;
        }

        int hitCount = 0;

        for (int i = 0; i < count && hitCount < piercingShotMaxTargets; i++)
        {
            Collider2D hit = _piercingBuffer[i];
            if (hit == null)
            {
                continue;
            }

            if (!hit.TryGetComponent(out IEntity target) || target.IsDead)
            {
                continue;
            }

            FireArrowAt(target);
            DealDamage(target, piercingShotMultiplier);
            hitCount++;
        }
    }

    /// <summary>
    /// 속사. 체력이 가장 낮은 적 1체에게 rapidShotCount번 연속으로 타격 (도중에 죽으면 거기서 멈춤)
    /// </summary>
    protected override void UseSkill2()
    {
        IEntity target = GetLowestHpEntity(EnemyLayer);
        if (target == null || target.IsDead)
        {
            return;
        }

        for (int i = 0; i < rapidShotCount; i++)
        {
            if (target.IsDead)
            {
                break;
            }

            FireArrowAt(target);
            DealDamage(target, rapidShotMultiplier);
        }
    }

    // 관통 사격은 사거리 안에 적이 하나라도 있어야 쓸 수 있음
    protected override bool CanUseSkill1() => FindEntitiesInRange(EnemyLayer, _piercingBuffer) > 0;

    // 속사는 때릴 대상이 있어야 쓸 수 있음
    protected override bool CanUseSkill2() => GetLowestHpEntity(EnemyLayer) != null;

    public override string Skill1Name => "관통 사격";
    public override string Skill2Name => "속사";
}
