/*
MonsterStatData(기본값) + 레벨(스테이지)로 현재 체력과 보상 골드를 계산함
  현재 체력   = 기본체력   × (체력증가율   ^ 레벨)
  보상 골드   = 기본골드   × (골드증가율   ^ 레벨)
보스는 별도 배율(bossHpMultiplier / bossGoldMultiplier)을 추가로 곱함
*/

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 스테이지에 등장하는 몬스터. 레벨에 따라 체력과 보상이 지수로 커짐
/// </summary>
public class Monster : MonoBehaviour, IEntity
{
    [Header("데이터")]
    // 이 몬스터의 기본 스탯 SO
    [SerializeField] private MonsterStatData statData;

    // 몬스터 레벨(보통 스테이지 번호)
    [SerializeField] private int level = 1;

    [Header("보스 배율 (일반 몬스터는 1)")]
    // 보스일 때 체력에 추가로 곱할 배율
    [SerializeField] private float bossHpMultiplier = 1f;

    //보스일 때 보상 골드에 추가로 곱할 배율
    [SerializeField] private float bossGoldMultiplier = 1f;

    [Header("전투")]
    // 공격 간격(초)
    [SerializeField] private float attackInterval = 1.5f;

    // 공격이 닿는 거리
    [SerializeField] private float attackRange = 1.5f;

    // 공격할 대상 레이어 (캐릭터 레이어)
    [SerializeField] private LayerMask targetLayer;

    [Header("이동")]
    // 캐릭터가 이 거리 안에 있으면 타겟으로 잡는다 (사실상 화면 전체면 크게)
    [SerializeField] private float aggroRange = 50f;

    // 레벨 기준으로 계산된 실시간값
    private float _currentHP;
    private float _maxHP;
    private double _rewardGold;
    private float _attackPower;
    private float _moveSpeed;

    // 이동/타겟팅
    private Rigidbody2D _rigidbody;
    private IEntity _target;
    private Transform _targetTf;

    // 풀링 때문에 "파괴"가 아니라 "비활성"마다 공격 루프를 멈춰야 해서 CTS를 직접 관리
    private CancellationTokenSource _attackCts;
    private ContactFilter2D _targetFilter;
    private readonly Collider2D[] _targetBuffer = new Collider2D[16];

    // 이 몬스터가 죽었을 때 발생. 인자로 자신을 넘김
    public event Action<Monster> Died;

    public float CurrentHP => _currentHP;

    public float MaxHP => _maxHP;

    // 이미 죽었는지 여부
    public bool IsDead => _currentHP <= 0f;

    public double RewardGold => _rewardGold;

    //몬스터 종류
    public MonsterKind Kind => statData != null ? statData.Kind : MonsterKind.Normal;

    public MonsterStatData StatData => statData;

    public int Level => level;

    // 현재 레벨 기준 공격력
    public float AttackPower => _attackPower;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        Recalculate();
        _currentHP = _maxHP;
    }

    private void OnEnable()
    {
        // 풀링으로 다시 켜질때 체력을 가득 채움
        _currentHP = _maxHP;
        _target = null;
        _targetTf = null;

        // 자동 공격 루프 시작 (이번 활성화 동안만 유효한 토큰)
        _attackCts = new CancellationTokenSource();
        RunAttackLoop(_attackCts.Token).Forget();

        OnSpawned();
    }

    private void FixedUpdate()
    {
        if (IsDead)
        {
            return;
        }

        // 타겟이 없거나 죽었으면 가장 가까운 캐릭터를 다시 잡는다
        if (_target == null || _target.IsDead || _targetTf == null)
        {
            AcquireTarget();
        }

        if (_target == null || _targetTf == null)
        {
            return;
        }

        Vector2 pos = _rigidbody != null ? _rigidbody.position : (Vector2)transform.position;
        Vector2 targetPos = _targetTf.position;

        // 사거리 밖이면 다가가고, 사거리 안이면 멈춘다 (공격은 공격 루프가 담당)
        if (Vector2.Distance(pos, targetPos) > attackRange)
        {
            Vector2 next = Vector2.MoveTowards(pos, targetPos, _moveSpeed * Time.fixedDeltaTime);
            if (_rigidbody != null)
            {
                _rigidbody.MovePosition(next);
            }
            else
            {
                transform.position = next;
            }
        }
    }

    /// <summary>
    /// aggroRange 안에서 가장 가까운 살아있는 캐릭터를 타겟으로 잡는다.
    /// </summary>
    private void AcquireTarget()
    {
        _target = null;
        _targetTf = null;

        _targetFilter.useTriggers = true;
        _targetFilter.SetLayerMask(targetLayer);
        int count = Physics2D.OverlapCircle(transform.position, aggroRange, _targetFilter, _targetBuffer);

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

            float sqr = ((Vector2)hit.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                _target = entity;
                _targetTf = hit.transform;
            }
        }
    }

    private void OnDisable()
    {
        // 비활성(풀 반환 / 파괴) 시 공격 루프 정지
        if (_attackCts != null)
        {
            _attackCts.Cancel();
            _attackCts.Dispose();
            _attackCts = null;
        }
    }

    /// <summary>
    /// 레벨을 바꾸고 체력, 보상을 다시 계산함 (풀에서 꺼내 재사용할 때 호출)
    /// </summary>
    /// <param name="newLevel">몬스터 레벨 (보통 스테이지 번호)</param>
    public void SetLevel(int newLevel)
    {
        level = Mathf.Max(1, newLevel);
        Recalculate();
        _currentHP = _maxHP;
    }

    /// <summary>
    /// 현재 레벨 기준으로 최대 체력과 보상 골드를 계산한다
    /// 참고: 1레벨을 기본값으로 두고 싶으면 지수를 (level - 1)로 바꿈
    /// </summary>
    protected virtual void Recalculate()
    {
        if (statData == null)
        {
            _maxHP = 1f;
            _rewardGold = 0d;
            _attackPower = 0f;
            _moveSpeed = 0f;
            return;
        }

        int safeLevel = Mathf.Max(0, level);
        bool isBoss = statData.Kind == MonsterKind.Boss;

        float hp = statData.BaseHp * Mathf.Pow(statData.HpGrowthPerLevel, safeLevel);
        if (isBoss)
        {
            hp *= bossHpMultiplier;
        }
        _maxHP = hp;

        // 공격력도 체력과 같은 증가율로 레벨 스케일 (시트에 따로 컬럼 필요하면 나중에 분리)
        _attackPower = statData.BaseAttack * Mathf.Pow(statData.HpGrowthPerLevel, safeLevel);

        _moveSpeed = statData.MoveSpeed;

        double gold = statData.BaseGold * System.Math.Pow(statData.GoldGrowthPerLevel, safeLevel);
        if (isBoss)
        {
            gold *= bossGoldMultiplier;
        }
        _rewardGold = gold;
    }

    /// <summary>
    /// 자동 공격 루프. attackInterval 마다 사거리 안 캐릭터 1명을 공격한다.
    /// </summary>
    /// <param name="token">비활성/파괴 시 루프를 멈추는 취소 토큰</param>
    private async UniTaskVoid RunAttackLoop(CancellationToken token)
    {
        while (!IsDead)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(attackInterval), cancellationToken: token);

            if (IsDead)
            {
                break;
            }

            PerformAttack();
        }
    }

    /// <summary>
    /// 실제 공격. 기본은 사거리 안 가장 가까운 캐릭터에게 AttackPower 만큼 피해.
    /// 다른 방식(범위 공격 등)이 필요하면 하위 클래스에서 override 한다.
    /// [2D] Physics2D.OverlapCircle 사용.
    /// </summary>
    protected virtual void PerformAttack()
    {
        _targetFilter.useTriggers = true;
        _targetFilter.SetLayerMask(targetLayer);
        int count = Physics2D.OverlapCircle(transform.position, attackRange, _targetFilter, _targetBuffer);

        IEntity nearest = null;
        float nearestSqr = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _targetBuffer[i];
            if (hit == null)
            {
                continue;
            }

            if (!hit.TryGetComponent(out IEntity target) || target.IsDead)
            {
                continue;
            }

            float sqr = (hit.transform.position - transform.position).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = target;
            }
        }

        if (nearest != null)
        {
            nearest.TakeDamage(_attackPower);
            OnAttack();
        }
    }

    // 공격 직후 훅 (공격 모션, 사운드 등)
    protected virtual void OnAttack() { }

    // IEntity

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
    /// 몬스터 회복 (독?디버프 해제?등 연출 등에서 사용) 최대 체력을 안넘음
    /// </summary>
    /// <param name="amount">회복량</param>
    public virtual void Heal(float amount)
    {
        if (IsDead)
        {
            return;
        }

        _currentHP = Mathf.Min(_maxHP, _currentHP + Mathf.Max(0f, amount));
    }

    /// <summary>
    /// 사망 처리. Died 이벤트를 발생시키고 게임오브젝트를 비활성화 (풀반환)
    /// </summary>
    public virtual void Die()
    {
        _currentHP = 0f;
        OnDied();
        Died?.Invoke(this);
        gameObject.SetActive(false);
    }

    // 등장 연출
    protected virtual void OnSpawned() { }

    /// <summary>피격 직후 훅 (피격 이펙트, 데미지 숫자 등)</summary>
    /// <param name="amount">실제로 받은 피해량</param>
    protected virtual void OnDamaged(float amount) { }

    // 사망 연출
    protected virtual void OnDied() { }
}
