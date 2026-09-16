// 작성자: 김주연
/*
팔라딘 - 탱커형 근접 물리 딜러. 체력이 높고 치명타는 낮은 대신, 스스로 버티는 스킬을 가짐
PhysicDealer(전사)는 그대로 두고, CharacterBase를 직접 상속해서 독자적으로 구현함
(자힐/피해감소 버프처럼 전사한테는 없는 훅이 필요해서 새 클래스로 분리)
*/

using UnityEngine;

/// <summary>
/// 근접 탱커형 캐릭터. 스킬1은 공격+자힐, 스킬2는 일정 시간 받는 피해를 줄이는 자기 버프
/// </summary>
public class PaladinDealer : CharacterBase
{
    [Header("스킬1: 방패 강타")]
    [Tooltip("방패 강타 데미지 배율 (평타 대비)")]
    [SerializeField] private float shieldBashMultiplier = 2f;

    [Tooltip("방패 강타 성공 시 자기 최대체력 대비 회복 비율 (0.1 = 10%)")]
    [SerializeField] private float shieldBashHealRatio = 0.1f;

    [Header("스킬2: 수호의 방패")]
    [Tooltip("받는 피해 감소가 지속되는 시간(초)")]
    [SerializeField] private float guardDuration = 5f;

    [Tooltip("받는 피해 감소율 (0.5 = 50% 감소)")]
    [Range(0f, 1f)]
    [SerializeField] private float guardDamageReduction = 0.5f;

    // 수호의 방패 남은 지속시간. 0 이하면 꺼진 상태
    private float _guardRemainingTime;

    private void Update()
    {
        // 수호의 방패 지속시간 카운트다운 (자동공격/스킬 루프랑 별개로 매 프레임 그냥 감소만 시킴)
        if (_guardRemainingTime > 0f)
        {
            _guardRemainingTime -= Time.deltaTime;
        }
    }

    /// <summary>
    /// 방패 강타. 체력이 가장 낮은 적 1체를 세게 때리고, 그 자리에서 자기 체력도 회복
    /// </summary>
    protected override void UseSkill1()
    {
        IEntity target = GetLowestHpEntity(EnemyLayer);
        if (target == null || target.IsDead)
        {
            return;
        }

        target.TakeDamage(Power * shieldBashMultiplier);
        Heal(MaxHP * shieldBashHealRatio);
    }

    /// <summary>
    /// 수호의 방패. guardDuration 동안 받는 피해를 guardDamageReduction만큼 줄인다 (자기 버프라 대상 필요 없음)
    /// </summary>
    protected override void UseSkill2()
    {
        _guardRemainingTime = guardDuration;
    }

    /// <summary>
    /// 피해를 받을 때 수호의 방패가 켜져있으면 미리 깎은 뒤 부모(CharacterBase)의 처리로 넘긴다
    /// </summary>
    /// <param name="amount">원래 받을 피해량</param>
    public override void TakeDamage(float amount)
    {
        float reduced = _guardRemainingTime > 0f ? amount * (1f - guardDamageReduction) : amount;
        base.TakeDamage(reduced);
    }

    // 방패 강타는 때릴 대상이 있어야 쓸 수 있음
    protected override bool CanUseSkill1() => GetLowestHpEntity(EnemyLayer) != null;

    // 수호의 방패는 자기 버프라 적 유무랑 상관없이 항상 사용 가능
    protected override bool CanUseSkill2() => true;
}
