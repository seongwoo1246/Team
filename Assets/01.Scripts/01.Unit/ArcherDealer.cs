/*
궁수 - 원거리 다단히트형 물리 딜러. 치명타율이 높고 체력은 낮은 대신, 스킬로 여러 대상/여러 발을 때림
PhysicDealer(전사)는 그대로 두고, CharacterBase를 직접 상속해서 독자적으로 구현함
*/

using UnityEngine;

/// <summary>
/// 원거리 딜러 캐릭터. 스킬1은 관통 사격(사거리 안 여러 적 동시 타격), 스킬2는 단일 대상 연속 사격
/// </summary>
public class ArcherDealer : CharacterBase
{
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

        float damage = Power * piercingShotMultiplier;
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

            target.TakeDamage(damage);
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

        float damage = Power * rapidShotMultiplier;
        for (int i = 0; i < rapidShotCount; i++)
        {
            if (target.IsDead)
            {
                break;
            }

            target.TakeDamage(damage);
        }
    }

    // 관통 사격은 사거리 안에 적이 하나라도 있어야 쓸 수 있음
    protected override bool CanUseSkill1() => FindEntitiesInRange(EnemyLayer, _piercingBuffer) > 0;

    // 속사는 때릴 대상이 있어야 쓸 수 있음
    protected override bool CanUseSkill2() => GetLowestHpEntity(EnemyLayer) != null;
}
