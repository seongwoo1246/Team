/*
MonsterStatData(기본값) + 레벨(스테이지)로 현재 체력을 계산함
  현재 체력 = 기본체력 × (체력증가율 ^ 레벨)
보스는 별도 배율(bossHpMultiplier)을 추가로 곱함

(골드 보상은 더 이상 몬스터 개별로 안 줌 - GoldWallet이 분당 고정 골드로 지급하는 방식으로 바뀌어서
 base_gold/gold_growth_per_level 관련 필드는 전부 제거함)
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

    [Header("장비 드랍")]
    [Tooltip("죽었을 때 장비가 드랍될 확률 (0~1). 0.02 = 2%")]
    [SerializeField] private float equipmentDropChance = 0.001f;

    [Tooltip("드랍 가능한 장비 후보들. 죽을 때 이 중 하나를 무작위로 골라 1~10% 랜덤 옵션으로 드랍함")]
    [SerializeField] private EquipmentData[] possibleDrops;

    // 레벨 기준으로 계산된 실시간값
    private float _currentHP;
    private float _maxHP;
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

    // true면 공격 동작은 하되 캐릭터에게 실제 피해를 주지 않음 (파밍처럼 캐릭터 체력을 관리하지 않는 모드용)
    private bool _isHarmless;

    // 이 몬스터가 죽었을 때 발생. 인자로 자신을 넘김
    public event Action<Monster> Died;

    // 이 몬스터가 장비를 드랍했을 때 발생. 인자 = 드랍된 장비 인스턴스 (인벤토리 시스템이 구독해서 가져가는 용도)
    public event Action<EquippedItem> EquipmentDropped;

    public float CurrentHP => _currentHP;

    public float MaxHP => _maxHP;

    // 이미 죽었는지 여부
    public bool IsDead => _currentHP <= 0f;

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
        _isHarmless = false;

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
    /// 이 몬스터가 캐릭터에게 실제 피해를 줄지 정한다
    /// 파밍처럼 캐릭터 체력을 관리하지 않는 모드에서 스포너가 소환 시점에 호출
    /// harmless여도 공격 동작(OnAttack 훅)은 그대로 일어나고, 실제 데미지만 안들어감
    /// </summary>
    /// <param name="harmless">true면 공격해도 피해를 주지 않음</param>
    public void SetHarmless(bool harmless)
    {
        _isHarmless = harmless;
    }

    /// <summary>
    /// 현재 레벨 기준으로 최대 체력을 계산한다
    /// 참고: 1레벨을 기본값으로 두고 싶으면 지수를 (level - 1)로 바꿈
    /// </summary>
    protected virtual void Recalculate()
    {
        if (statData == null)
        {
            _maxHP = 1f;
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
    }

    /// <summary>
    /// 자동 공격 루프. attackInterval 마다 사거리 안 캐릭터 1명을 공격
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
    /// 실제 공격. 기본은 사거리 안 가장 가까운 캐릭터에게 AttackPower 만큼 피해
    /// 다른 방식(범위 공격 등)이 필요하면 하위 클래스에서 override 한다
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
            if (!_isHarmless)
            {
                nearest.TakeDamage(_attackPower);
            }

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
    /// 사망 처리. 낮은 확률로 장비를 드랍시키고 Died 이벤트를 발생시킨 뒤 게임오브젝트를 비활성화 (풀반환)
    /// 장비 드랍이 Died보다 먼저 발동해야 함 - 구독자가 보통 Died 핸들러 안에서 구독을 해제하는데
    /// 순서가 반대면 EquipmentDropped가 발동하기도 전에 구독이 풀려서 이벤트를 놓치게 됨
    /// </summary>
    public virtual void Die()
    {
        _currentHP = 0f;
        OnDied();
        TryDropEquipment();
        Died?.Invoke(this);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 죽은 것으로 치지 않고 그냥 회수한다 (스테이지/파밍 모드 전환 등으로 강제로 필드를 비울 때 사용).
    /// Die()와 달리 보상이 지급되지 않도록 Died/EquipmentDropped 구독을 전부 정리한 뒤 비활성화
    /// (풀링으로 재사용되는 인스턴스라, 구독을 안 지우면 다음에 재사용될 때 예전 구독이 중복으로 남아있게 됨)
    /// (Die()를 안 거치므로 장비도 드랍 안 됨)
    /// </summary>
    public void Despawn()
    {
        Died = null;
        EquipmentDropped = null;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// possibleDrops 중 하나를 무작위로 골라 equipmentDropChance 확률로 장비를 드랍
    /// 드랍되면 1~10% 사이 랜덤 보너스로 EquippedItem을 만들어 EquipmentDropped 이벤트로 넘긴다
    /// </summary>
    private void TryDropEquipment()
    {
        if (possibleDrops == null || possibleDrops.Length == 0)
        {
            return;
        }

        if (UnityEngine.Random.value > equipmentDropChance)
        {
            return;
        }

        EquipmentData picked = possibleDrops[UnityEngine.Random.Range(0, possibleDrops.Length)];
        if (picked == null)
        {
            return;
        }

        float rollPercent = UnityEngine.Random.Range(1f, 10f);
        EquippedItem dropped = new EquippedItem(picked, rollPercent);
        EquipmentDropped?.Invoke(dropped);
    }

    // 등장 연출
    protected virtual void OnSpawned() { }

    /// <summary>피격 직후 훅 (피격 이펙트, 데미지 숫자 등)</summary>
    /// <param name="amount">실제로 받은 피해량</param>
    protected virtual void OnDamaged(float amount) { }

    // 사망 연출
    protected virtual void OnDied() { }
}
