/*
캐릭터(물리 딜러 / 마법 딜러 / 힐러)의 공통 부모

설계 원칙 (팀 규칙):
  - 전투의 전체 흐름(자동 공격 루프, 데미지 처리 순서)은 여기서 고정
      아래 virtual 함수만 override 한다.
      PerformAttack      : 실제 공격/힐 동작
      OnBeforeAttack     : 공격 직전 (이펙트, 사운드 등)
      OnAfterAttack      : 공격 직후 (쿨다운 연출 등)
      OnDamaged / OnDied : 피격/사망 반응
  - 스탯은 저장하지 않고 StatCalculator로 현재 레벨에 맞게 계산해서 보유
*/

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 캐릭터 공통 기반 클래스. 프리팹에 붙여 사용하며, 하위 클래스가 공격 방식을 정의
/// </summary>
public class CharacterBase : MonoBehaviour, IEntity
{
    // 공격 간격 기본값(초)
    private const float DEFAULT_ATTACK_INTERVAL = 1f;

    // 공격 사거리
    private const float DEFAULT_ATTACK_RANGE = 3f;

    // 한번에 탐지할 수 있는 최대 대상 수 (OverlapCircle 결과 버퍼 크기)
    protected const int MAX_TARGET_BUFFER = 32;

    [Header("데이터")]
    // 이 캐릭터의 기본 스탯 SO. CSV 임포터가 만든 에셋을 넣음
    [SerializeField] private BaseStatData statData;

    [Header("전투 설정")]
    // 자동 공격 간격(초)
    [SerializeField] private float attackInterval = DEFAULT_ATTACK_INTERVAL;

    // 공격이 닿는 거리
    [SerializeField] private float attackRange = DEFAULT_ATTACK_RANGE;

    // 적으로 인식할 레이어 (몬스터 레이어)
    [SerializeField] private LayerMask enemyLayer;

    // 아군으로 인식할 레이어 (캐릭터 레이어). 힐러가 사용해야함
    [SerializeField] private LayerMask allyLayer;

    // 파티 강화 트랙 레벨 기준으로 계산된 실시간 스탯 (전투하면서 계속 바뀌니까 직렬화 안함)
    private float _currentHP;
    private float _currentMaxHP;
    private float _currentPower;

    // AttackSpeed 트랙이 반영된 실제 공격 간격 (attackInterval 을 속도 계수로 나눈 값)
    private float _currentAttackInterval;

    // 파티 강화 시스템 (트랙 레벨을 읽고, 강화 이벤트를 구독)
    private UpgradeSystem _upgradeSystem;

    // OverlapCircle 결과를 매번 새로 만들지 않도록 재사용하는 버퍼 (2D)
    private readonly Collider2D[] _targetBuffer = new Collider2D[MAX_TARGET_BUFFER];

    // 2D 물리 탐지에 쓰는 필터 (struct라 매번 만들어도 GC 없음)
    private ContactFilter2D _contactFilter;

    public float CurrentHP => _currentHP;

    public float MaxHP => _currentMaxHP;

    public bool IsDead => _currentHP <= 0f;

    // 치명타 기대값이 반영된 현재 공격력(힐러는 힐량)
    public float Power => _currentPower;

    // 이 캐릭터의 기본 스탯 SO
    public BaseStatData StatData => statData;

    // 공격 방식 (물리 / 마법 / 힐)
    public AttackType AttackType => statData != null ? statData.AttackType : AttackType.Physical;

    // 공격 사거리
    protected float AttackRange => attackRange;

    // 적 레이어 마스크
    protected LayerMask EnemyLayer => enemyLayer;

    // 아군 레이어 마스크
    protected LayerMask AllyLayer => allyLayer;

    private void Awake()
    {
        RecalculateStats();
        _currentHP = _currentMaxHP;
    }

    private void Start()
    {
        // 파티 강화 시스템 구독 (트랙이 오르면 스탯 재계산)
        _upgradeSystem = UpgradeSystem.instance;
        if (_upgradeSystem != null)
        {
            _upgradeSystem.TrackUpgraded += OnTrackUpgraded;
        }

        // Awake 시점엔 UpgradeSystem 이 아직 없었을 수 있으니 한 번 더 계산
        RecalculateStats();
        _currentHP = _currentMaxHP;

        // 오브젝트가 파괴되면 루프도 자동으로 멈추도록 유니태스크 취소 토큰
        RunAutoAttackLoop(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private void OnDestroy()
    {
        if (_upgradeSystem != null)
        {
            _upgradeSystem.TrackUpgraded -= OnTrackUpgraded;
        }
    }

    /// <summary>
    /// 파티 트랙이 강화됐을 때. 스탯을 다시 계산하되 체력 비율은 유지
    /// </summary>
    private void OnTrackUpgraded(UpgradeTrack track)
    {
        float hpRatio = _currentMaxHP > 0f ? _currentHP / _currentMaxHP : 1f;
        RecalculateStats();
        _currentHP = _currentMaxHP * hpRatio;
    }

    /// <summary>
    /// 현재 파티 트랙 레벨에 맞는 최대 체력과 실효 공격력(힐량)을 계산해 저장.
    /// 공격력=Power 트랙, 체력=Hp 트랙, 치명타=Crit 트랙 을 각각 사용한다.
    /// 계산 방식을 바꾸고 싶으면 하위 클래스에서 override 한다.
    /// </summary>
    protected virtual void RecalculateStats()
    {
        if (statData == null)
        {
            _currentMaxHP = 1f;
            _currentPower = 0f;
            _currentAttackInterval = attackInterval;
            return;
        }

        int powerLevel = _upgradeSystem != null ? _upgradeSystem.GetLevel(UpgradeTrack.Power) : 0;
        int hpLevel = _upgradeSystem != null ? _upgradeSystem.GetLevel(UpgradeTrack.Hp) : 0;
        int critChanceLevel = _upgradeSystem != null ? _upgradeSystem.GetLevel(UpgradeTrack.Crit) : 0;
        int critDamageLevel = _upgradeSystem != null ? _upgradeSystem.GetLevel(UpgradeTrack.CritDamage) : 0;

        _currentMaxHP = StatCalculator.GetMaxHP(statData, hpLevel);
        _currentPower = StatCalculator.GetEffectiveStatValue(statData, powerLevel, critChanceLevel, critDamageLevel);

        float speedFactor = _upgradeSystem != null ? _upgradeSystem.GetAttackSpeedFactor() : 1f;
        _currentAttackInterval = Mathf.Max(0.05f, attackInterval / Mathf.Max(0.01f, speedFactor));
    }

    /// <summary>
    /// 자동 공격 루프. attackInterval 마다 한 번씩 공격 사이클을 돔
    /// </summary>
    /// <param name="token">파괴 시 루프를 멈추는 취소 토큰</param>
    private async UniTaskVoid RunAutoAttackLoop(CancellationToken token)
    {
        while (!IsDead)
        {
            // _currentAttackInterval 은 AttackSpeed 강화 시 바뀌므로 매 반복마다 읽는다
            await UniTask.Delay(TimeSpan.FromSeconds(_currentAttackInterval), cancellationToken: token);

            if (IsDead)
            {
                break;
            }

            DoAttackCycle();
        }
    }

    /// <summary>
    /// 공격 한 사이클의 고정 순서: 사전 훅 → 실제 공격 → 사후 훅.
    /// 이 순서 자체는 건드리지 말고 바꾸거나 추가할거면 알려주세요
    /// </summary>
    protected virtual void DoAttackCycle()
    {
        OnBeforeAttack();
        PerformAttack();
        OnAfterAttack();
    }

    /// <summary>
    /// 실제 공격 동작. 기본 구현은 사거리 안 가장 가까운 적 1체에게 Power 만큼 피해
    /// 마법 딜러(다수) / 힐러(회복)는 이 함수를 override
    /// </summary>
    protected virtual void PerformAttack()
    {
        IEntity target = GetNearestEntity(enemyLayer);
        if (target != null && !target.IsDead)
        {
            target.TakeDamage(_currentPower);
        }
    }

    // 공격 직전 훅. 기본은 아무것도 안함 (이펙트/사운드 추가용)
    protected virtual void OnBeforeAttack() { }

    // 공격 직후 훅. 기본은 아무것도 안함 (쿨다운 연출 등)
    protected virtual void OnAfterAttack() { }

    /// <summary>피해를 받은 직후 훅. (피격 이펙트, 넉백 등)</summary>
    /// <param name="amount">실제로 받은 피해량</param>
    protected virtual void OnDamaged(float amount) { }

    // 사망 처리 직후 훅. (사망 애니메이션, 드랍 등)
    protected virtual void OnDied() { }

    /// <summary>
    /// 사거리 안에서 주어진 레이어의 대상들을 찾아 가장 가까운 IEntity를 돌려준다.
    /// 할당 없는 2D 원형 탐지(OverlapCircle)를 사용한다.
    /// </summary>
    /// <param name="layer">탐지할 레이어 마스크</param>
    /// 가장 가까운 대상. 없으면 null
    protected IEntity GetNearestEntity(LayerMask layer)
    {
        int count = OverlapCircle(layer, _targetBuffer);

        IEntity nearest = null;
        float nearestSqr = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _targetBuffer[i];
            if (hit == null)
            {
                continue;
            }

            if (!hit.TryGetComponent(out IEntity entity) || entity.IsDead)
            {
                continue;
            }

            float sqr = (hit.transform.position - transform.position).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = entity;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 사거리 안에서 주어진 레이어의 대상들을 버퍼에 채우고 개수를 돌려줌
    /// 마법 딜러(다수 공격), 힐러(다수 회복)가 사용
    /// </summary>
    /// <param name="layer">탐지할 레이어 마스크</param>
    /// <param name="buffer">결과를 담을 배열 (재사용 버퍼 권장)</param>
    /// 채워진 대상 수
    protected int FindEntitiesInRange(LayerMask layer, Collider2D[] buffer)
    {
        return OverlapCircle(layer, buffer);
    }

    /// <summary>
    /// 2D 원형 범위 안에서 해당 레이어의 콜라이더를 buffer에 채우고 개수를 돌려주는 공통 함수.
    /// </summary>
    private int OverlapCircle(LayerMask layer, Collider2D[] buffer)
    {
        _contactFilter.useTriggers = true;
        _contactFilter.SetLayerMask(layer);
        return Physics2D.OverlapCircle(transform.position, attackRange, _contactFilter, buffer);
    }

    // IEntity 구현

    /// <summary>
    /// 피해를 입는다. 체력이 0 이하가 되면 Die()를 호출
    /// </summary>
    /// <param name="amount">받을 피해량</param>
    public virtual void TakeDamage(float amount)
    {
        if (IsDead)
        {
            return;
        }

        float damage = Mathf.Max(0f, amount);
        _currentHP = Mathf.Max(0f, _currentHP - damage);
        OnDamaged(damage);

        if (_currentHP <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// 체력을 회복한다. 최대 체력을 넘지 않는다. 죽은 대상은 회복되지 않는다
    /// </summary>
    /// <param name="amount">회복량</param>
    public virtual void Heal(float amount)
    {
        if (IsDead)
        {
            return;
        }

        float heal = Mathf.Max(0f, amount);
        _currentHP = Mathf.Min(_currentMaxHP, _currentHP + heal);
    }

    /// <summary>
    /// 사망 처리. 게임오브젝트를 비활성화 풀로돌려보내
    /// </summary>
    public virtual void Die()
    {
        _currentHP = 0f;
        OnDied();
        gameObject.SetActive(false);
    }
}
