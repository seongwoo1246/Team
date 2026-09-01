/*
힐러
공격 대신, 사거리 안에서 체력 비율이 가장 낮은 아군 1명을 회복함
힐량은 CharacterBase.Power (StatCalculator가 계산한 값)를 그대로 사용
*/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아군을 회복시키는 지원 캐릭터 적공격x
/// </summary>
public class Healer : CharacterBase
{
    [Header("힐러 옵션")]
    [Tooltip("체력이 이 비율 미만인 아군만 힐 대상으로 삼는다. 1.0이면 항상 힐")]
    [Range(0.1f, 1f)]
    [SerializeField] private float healThreshold = 0.95f;

    [Tooltip("힐 이펙트!")]
    [SerializeField] private ParticleSystem healEffect;

    [Header("스킬1: 전체 회복")]
    [Tooltip("전체 회복량 배율 (평소 힐량 대비). 사거리 안 아군 전원에게 동시 적용")]
    [SerializeField] private float massHealMultiplier = 0.6f;

    // OverlapCircle 결과 재사용 버퍼
    private readonly Collider2D[] _allyBuffer = new Collider2D[MAX_TARGET_BUFFER];

    // 전체 회복(스킬1)용 OverlapCircle 결과 재사용 버퍼. 평소 힐 탐색용(_allyBuffer)이랑 따로 둠
    private readonly Collider2D[] _massHealBuffer = new Collider2D[MAX_TARGET_BUFFER];

    /// <summary>
    /// 힐러의 공격 = 회복. 가장 다친 아군 1명을 Power 만큼 회복한다.
    /// </summary>
    protected override void PerformAttack()
    {
        IEntity target = FindMostWoundedAlly();
        if (target == null)
        {
            return;
        }

        target.Heal(Power);

        if (healEffect != null)
        {
            healEffect.Play();
        }
    }

    /// <summary>
    /// 사거리 안 아군 중 체력 비율이 가장 낮은 대상을 찾음
    /// healThreshold 이상으로 가득 찬 아군은 제외 자기 자신도 대상에 포함하게했음
    /// </summary>
    /// 힐이 필요한 아군이 없으면 null
    private IEntity FindMostWoundedAlly()
    {
        // 이름만 Find지 Unity의 Find 계열 API를 안씀! 걱정마세요 (Physics2D.OverlapCircle) 이런거임
        int count = FindEntitiesInRange(AllyLayer, _allyBuffer);
        if (count <= 0)
        {
            return null;
        }

        IEntity mostWounded = null;
        float lowestRatio = healThreshold;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _allyBuffer[i];
            if (hit == null)
            {
                continue;
            }

            if (!hit.TryGetComponent(out IEntity ally) || ally.IsDead || ally.MaxHP <= 0f)
            {
                continue;
            }

            float ratio = ally.CurrentHP / ally.MaxHP;
            if (ratio < lowestRatio)
            {
                lowestRatio = ratio;
                mostWounded = ally;
            }
        }

        return mostWounded;
    }

    /// <summary>스킬1: 전체 회복. 사거리 안 아군 전원을 동시에 회복</summary>
    protected override void UseSkill1()
    {
        int count = FindEntitiesInRange(AllyLayer, _massHealBuffer);
        if (count <= 0)
        {
            return;
        }

        float healAmount = Power * massHealMultiplier;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _massHealBuffer[i];
            if (hit == null)
            {
                continue;
            }

            if (!hit.TryGetComponent(out IEntity ally) || ally.IsDead)
            {
                continue;
            }

            ally.Heal(healAmount);
        }
    }

    /// <summary>스킬2: 부활. 죽어있는 아군 1명을 찾아 되살림</summary>
    protected override void UseSkill2()
    {
        CharacterBase deadAlly = FindDeadAlly();
        if (deadAlly == null)
        {
            return;
        }

        deadAlly.Revive();
    }

    /// <summary>부활은 죽어있는 아군이 있을 때만 사용 가능 (없으면 버튼이 눌려도 쿨다운이 안깎임)</summary>
    protected override bool CanUseSkill2()
    {
        return FindDeadAlly() != null;
    }

    /// <summary>
    /// 죽어서 비활성 상태인 아군을 찾는다. 물리 탐지(OverlapCircle)는 죽어서 꺼진 오브젝트를
    /// 못찾기 때문에 CharacterBase가 들고 있는 전체 캐릭터 목록에서 직접 찾음
    /// </summary>
    private CharacterBase FindDeadAlly()
    {
        IReadOnlyList<CharacterBase> all = CharacterBase.AllCharacters;

        for (int i = 0; i < all.Count; i++)
        {
            CharacterBase character = all[i];
            if (character != null && character != this && character.IsDead)
            {
                return character;
            }
        }

        return null;
    }
}
