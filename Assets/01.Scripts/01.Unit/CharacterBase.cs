/*
캐릭터(물리 딜러 / 마법 딜러 / 힐러)의 공통 부모

설계 원칙 (팀 규칙):
  - 전투의 전체 흐름(자동 공격 루프, 데미지 처리 순서)은 여기서 고정
      아래 virtual 함수만 override 한다.
      PerformAttack      : 실제 공격/힐 동작
      OnBeforeAttack     : 공격 직전 (이펙트, 사운드 등)
      OnAfterAttack      : 공격 직후 (쿨다운 연출 등)
      OnDamaged / OnDied : 피격/사망 반응
      UseSkill1 / UseSkill2 : 스킬 버튼(수동) 또는 오토 스킬(자동)로 쿨다운마다 발동하는 스킬
  - 스탯은 저장하지 않고 StatCalculator로 현재 레벨에 맞게 계산해서 보유
  - 스킬은 TryUseSkill1()/TryUseSkill2()
    스킬 버튼 UI랑 오토 스킬 루프 둘 다 이 함수를 거쳐가서 쿨다운이 항상 하나로 관리됨
*/

using System;
using System.Collections.Generic;
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

    [Header("스킬 설정")]
    [Tooltip("스킬1 재사용 대기시간(초). 평소 공격보다 조금 더 센 즉발 스킬용 (0 이하면 스킬1 없음)")]
    [SerializeField] private float skill1Cooldown = 8f;

    [Tooltip("스킬2 재사용 대기시간(초). 쿨다운이 긴 대신 강력한 필살기용 (0 이하면 스킬2 없음)")]
    [SerializeField] private float skill2Cooldown = 20f;

    // 씬에 존재하는 모든 캐릭터 목록. 죽으면 SetActive(false)로 꺼져서 물리 탐지(OverlapCircle)에 안 잡히기 때문에,
    // "죽은 아군 찾기"(힐러 부활 스킬 등)처럼 비활성 상태도 찾아야 하는 경우 이 목록을 대신 쓴다
    private static readonly List<CharacterBase> _allCharacters = new List<CharacterBase>();

    // 스킬을 자동으로 쓸지 여부. 캐릭터 전체 공통이라 static (오토 스킬 토글 UI가 이 값 하나만 바꿈)
    private static bool _autoSkillEnabled = true;

    // 오토 스킬 폴링 간격(초). 버튼으로 스킬을 써도 이 주기 안에서 자동 루프가 쿨다운을 다시 확인함
    private const float AUTO_SKILL_POLL_INTERVAL = 0.2f;

    // 마지막으로 스킬을 사용한 시각(Time.time 기준). 시작하자마자 바로 쓸 수 있게 아주 예전 값으로 초기화
    private float _skill1LastUsedTime = float.NegativeInfinity;
    private float _skill2LastUsedTime = float.NegativeInfinity;

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

    // 씬에 존재하는 모든 캐릭터 (죽어서 비활성 상태인 것도 포함). 읽기 전용으로만 공개
    public static IReadOnlyList<CharacterBase> AllCharacters => _allCharacters;

    // 스킬 자동 사용 여부 (캐릭터 전체 공통). 오토 스킬 토글 UI가 이 값을 설정
    public static bool AutoSkillEnabled
    {
        get => _autoSkillEnabled;
        set => _autoSkillEnabled = value;
    }

    // 스킬1 쿨다운 진행률. 0 = 바로 사용 가능, 1 = 방금 사용해서 꽉 참 (버튼의 원형 게이지가 이 값을 읽음)
    public float Skill1CooldownRatio => skill1Cooldown > 0f ? Mathf.Clamp01(1f - (Time.time - _skill1LastUsedTime) / skill1Cooldown) : 0f;

    // 스킬2 쿨다운 진행률. 0 = 바로 사용 가능, 1 = 방금 사용해서 꽉 참
    public float Skill2CooldownRatio => skill2Cooldown > 0f ? Mathf.Clamp01(1f - (Time.time - _skill2LastUsedTime) / skill2Cooldown) : 0f;

    // 스킬1을 지금 바로 쓸 수 있는지 (쿨다운만 기준)
    public bool IsSkill1Ready => Skill1CooldownRatio <= 0f;

    // 스킬2를 지금 바로 쓸 수 있는지 (쿨다운만 기준)
    public bool IsSkill2Ready => Skill2CooldownRatio <= 0f;

    // 스킬1을 실제로 쓸 수 있는지 (쿨다운 + CanUseSkill1 조건 둘 다 만족해야 함). 버튼 UI가 이 값으로 사용 가능 여부를 표시
    public bool IsSkill1Usable => IsSkill1Ready && CanUseSkill1();

    // 스킬2를 실제로 쓸 수 있는지 (쿨다운 + CanUseSkill2 조건 둘 다 만족해야 함)
    public bool IsSkill2Usable => IsSkill2Ready && CanUseSkill2();

    private void Awake()
    {
        _allCharacters.Add(this);

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

        StartCombatLoops();
    }

    private void OnDestroy()
    {
        _allCharacters.Remove(this);

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
    /// 자동공격 루프 + 오토 스킬 루프를 전부 새로 시작한다.
    /// Start()와 Revive() 둘 다 여기를 호출 - 부활했을 때도 루프가 다시 돌아야 하기 때문
    /// </summary>
    private void StartCombatLoops()
    {
        CancellationToken token = this.GetCancellationTokenOnDestroy();
        RunAutoAttackLoop(token).Forget();
        RunAutoSkillLoopAsync(token).Forget();
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
    /// 오토 스킬 루프. AUTO_SKILL_POLL_INTERVAL마다 "지금 오토 스킬이 켜져있고 쿨다운이 다 찼는지"를 확인해서
    /// 다 찼으면 TryUseSkill1/2를 부른다. 버튼 클릭도 똑같이 TryUseSkill1/2를 거치기 때문에,
    /// 버튼으로 방금 쓴 스킬을 오토 루프가 또 바로 쓰는 일은 없다 (쿨다운을 공유해서 판단하므로)
    /// </summary>
    /// <param name="token">파괴 시 루프를 멈추는 취소 토큰</param>
    private async UniTaskVoid RunAutoSkillLoopAsync(CancellationToken token)
    {
        while (!IsDead)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(AUTO_SKILL_POLL_INTERVAL), cancellationToken: token);

            if (IsDead)
            {
                break;
            }

            if (!AutoSkillEnabled)
            {
                continue;
            }

            TryUseSkill1();
            TryUseSkill2();
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

    // 스킬1 (짧은 쿨다운). 기본은 아무것도 안함 - 하위 클래스가 override
    protected virtual void UseSkill1() { }

    // 스킬2 (긴 쿨다운, 강력한 버전). 기본은 아무것도 안함 - 하위 클래스가 override
    protected virtual void UseSkill2() { }

    // 스킬1을 지금 쓸 수 있는 상황인지 (쿨다운 말고 추가 조건). 기본은 항상 true - 필요하면 하위 클래스가 override
    // 예: 부활 스킬은 "죽은 아군이 있을 때만" 쓸 수 있게 하위 클래스에서 이 함수를 override
    protected virtual bool CanUseSkill1() => true;

    // 스킬2 버전. CanUseSkill1과 동일한 용도
    protected virtual bool CanUseSkill2() => true;

    /// <summary>
    /// 스킬1을 시도한다. 쿨다운이 다 찼고 CanUseSkill1() 조건도 만족하면 사용하고 true, 아니면 false
    /// 스킬 버튼 OnClick과 오토 스킬 루프 둘다 여기를 거쳐가는 유일한 진입점
    /// 조건이 안 맞으면 쿨다운을 소모하지 않는다 (예: 부활 대상이 없는데 눌러도 쿨다운 안 깎임)
    /// </summary>
    public bool TryUseSkill1()
    {
        if (IsDead || !IsSkill1Ready || !CanUseSkill1())
        {
            return false;
        }

        _skill1LastUsedTime = Time.time;
        UseSkill1();
        return true;
    }

    /// <summary>스킬2 버전. TryUseSkill1과 동일하게 동작</summary>
    public bool TryUseSkill2()
    {
        if (IsDead || !IsSkill2Ready || !CanUseSkill2())
        {
            return false;
        }

        _skill2LastUsedTime = Time.time;
        UseSkill2();
        return true;
    }

    /// <summary>
    /// 스킬1/스킬2 쿨다운을 즉시 초기화해서 바로 다시 쓸 수 있게 함
    /// 파밍 진입/챌린지 진입 시 StageManager가 파티 전원에게 호출
    /// </summary>
    public void ResetSkillCooldowns()
    {
        _skill1LastUsedTime = float.NegativeInfinity;
        _skill2LastUsedTime = float.NegativeInfinity;
    }

    /// <summary>피해를 받은 직후 훅. (피격 이펙트, 넉백 등)</summary>
    /// <param name="amount">실제로 받은 피해량</param>
    protected virtual void OnDamaged(float amount) { }

    // 사망 처리 직후 훅. (사망 애니메이션, 드랍 등)
    protected virtual void OnDied() { }

    /// <summary>
    /// 사거리 안에서 주어진 레이어의 대상들을 찾아 가장 가까운 IEntity를 돌려줌
    /// 할당 없는 2D 원형 탐지(OverlapCircle)를 사용
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
    /// 2D 원형 범위 안에서 해당 레이어의 콜라이더를 buffer에 채우고 개수를 돌려주는 공통 함수
    /// </summary>
    private int OverlapCircle(LayerMask layer, Collider2D[] buffer)
    {
        _contactFilter.useTriggers = true;
        _contactFilter.SetLayerMask(layer);
        return Physics2D.OverlapCircle(transform.position, attackRange, _contactFilter, buffer);
    }

    /// <summary>
    /// 캐릭터를 선택했을 때 Scene 뷰 공격/스킬 판정 반경을 원으로 표시
    /// 평타든 스킬1/스킬2든 전부 이 attackRange 하나로 판정해서 이 원이 실제 감지범위와 정확히 일치함
    /// (강타/파이어볼은 이 원 안에서 가장 가까운 1체, 휩쓸기/메테오/전체회복은 이 원 안 전체가 대상)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
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

    /// <summary>
    /// 죽은 캐릭터를 되살린다. 체력을 최대로 채우고 게임오브젝트를 다시 활성화
    /// 자동공격/스킬 루프는 죽을 때 while(!IsDead) 조건에 걸려 전부 완전히 끝나버리고 SetActive만으로는
    /// 다시 안 돌기 때문에, 여기서 직접 루프들을 재시작
    /// </summary>
    public void Revive()
    {
        RecalculateStats();
        _currentHP = _currentMaxHP;
        ResetSkillCooldowns();

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        StartCombatLoops();
    }
}
