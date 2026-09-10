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

    [Header("보스 광폭화 (Kind가 Boss일 때만 동작)")]
    [Tooltip("체력이 이 비율 이하로 떨어지면 광폭화 (0.3 = 30%)")]
    [SerializeField] private float enrageHpRatio = 0.3f;

    [Tooltip("광폭화 시 공격력/공격속도에 곱할 배율")]
    [SerializeField] private float enrageMultiplier = 1.5f;

    [Tooltip("광폭화 시 물들일 색 (밝은 빨강 추천 - 원래 색이 어두워도 눈에 띄게)")]
    [SerializeField] private Color enrageTintColor = new Color(1f, 0.15f, 0.15f, 1f);

    [Tooltip("광폭화 시 원래 색에서 enrageTintColor 쪽으로 얼마나 강하게 끌어당길지 (0=원래색 그대로, 1=완전히 틴트색)")]
    [Range(0f, 1f)]
    [SerializeField] private float enrageTintStrength = 0.6f;

    // 레벨 기준으로 계산된 실시간값
    private float _currentHP;
    private float _maxHP;
    private float _attackPower;
    private float _moveSpeed;

    // 한 번 광폭화되면 죽거나 풀에 반환될 때까지 계속 true로 유지됨
    private bool _isEnraged;

    // 이동/타겟팅
    private Rigidbody2D _rigidbody;
    private IEntity _target;
    private Transform _targetTf;

    // 광폭화 시 색 틴트를 입히기 위한 참조. 원래 색(예: 황금 고블린의 금색)을 기억해뒀다가 그 위에 곱함
    private SpriteRenderer _spriteRenderer;
    private Color _baseSpriteColor = Color.white;

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
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null)
        {
            _baseSpriteColor = _spriteRenderer.color;
        }

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

        // 광폭화 상태도 원래대로 초기화 (풀에서 재사용될 때 이전 생애의 광폭화가 안 남게)
        _isEnraged = false;
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = _baseSpriteColor;
        }

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
    /// aggroRange 안에서 PickTargetCollider() 기준으로 타겟을 잡는다 (기본은 가장 가까운 캐릭터)
    /// </summary>
    private void AcquireTarget()
    {
        _target = null;
        _targetTf = null;

        _targetFilter.useTriggers = true;
        _targetFilter.SetLayerMask(targetLayer);
        int count = Physics2D.OverlapCircle(transform.position, aggroRange, _targetFilter, _targetBuffer);

        Collider2D picked = PickTargetCollider(_targetBuffer, count, transform.position);
        if (picked != null && picked.TryGetComponent(out IEntity entity))
        {
            _target = entity;
            _targetTf = picked.transform;
        }
    }

    /// <summary>
    /// 후보 콜라이더들 중 실제로 노릴 대상 하나를 고른다. 기본은 가장 가까운 대상
    /// AcquireTarget(이동용)이랑 PerformAttack(공격용) 둘 다 이 함수를 거쳐가므로,
    /// 여기 하나만 override하면 이동/공격 타겟이 항상 일치하게 됨(예: 후열 우선 타겟팅)
    /// </summary>
    /// <param name="buffer">OverlapCircle로 찾은 콜라이더 후보들</param>
    /// <param name="count">buffer에서 앞쪽 유효한 개수</param>
    /// <param name="originPosition">거리 계산 기준이 될 이 몬스터의 현재 위치</param>
    /// 고른 대상의 콜라이더. 없으면 null
    protected virtual Collider2D PickTargetCollider(Collider2D[] buffer, int count, Vector2 originPosition)
    {
        Collider2D nearest = null;
        float nearestSqr = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = buffer[i];
            if (hit == null)
            {
                continue;
            }

            if (!hit.TryGetComponent(out IEntity entity) || entity.IsDead)
            {
                continue;
            }

            float sqr = ((Vector2)hit.transform.position - originPosition).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = hit;
            }
        }

        return nearest;
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
    /// 광폭화 중이면 간격이 enrageMultiplier만큼 줄어들어(공격속도 증가) 더 자주 공격함
    /// </summary>
    /// <param name="token">비활성/파괴 시 루프를 멈추는 취소 토큰</param>
    private async UniTaskVoid RunAttackLoop(CancellationToken token)
    {
        while (!IsDead)
        {
            // _isEnraged는 도중에 바뀔 수 있으므로 반복마다 다시 읽는다
            float effectiveInterval = _isEnraged ? attackInterval / enrageMultiplier : attackInterval;
            await UniTask.Delay(TimeSpan.FromSeconds(effectiveInterval), cancellationToken: token);

            if (IsDead)
            {
                break;
            }

            PerformAttack();
        }
    }

    /// <summary>
    /// 실제 공격. 기본은 PickTargetCollider() 기준(가장 가까운 캐릭터)에게 AttackPower 만큼 피해
    /// 범위 공격 등 완전히 다른 방식이 필요하면 이 함수를 통째로 override 하고,
    /// 대상 "누구를 고를지"만 바꾸고 싶으면 PickTargetCollider()만 override 한다
    /// [2D] Physics2D.OverlapCircle 사용.
    /// </summary>
    protected virtual void PerformAttack()
    {
        _targetFilter.useTriggers = true;
        _targetFilter.SetLayerMask(targetLayer);
        int count = Physics2D.OverlapCircle(transform.position, attackRange, _targetFilter, _targetBuffer);

        Collider2D picked = PickTargetCollider(_targetBuffer, count, transform.position);
        if (picked != null && picked.TryGetComponent(out IEntity target))
        {
            if (!_isHarmless)
            {
                target.TakeDamage(_attackPower);
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
        CheckEnrage();

        if (_currentHP <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// 보스(Kind == MonsterKind.Boss) 한정: 체력이 enrageHpRatio 이하로 떨어지면 공격력/공격속도를
    /// enrageMultiplier배로 올리고 스프라이트를 살짝 붉게 물들인다. 한 번 발동하면 죽거나 풀에
    /// 반환될 때까지(OnEnable에서 초기화됨) 계속 유지되고, 다시 발동하지 않는다
    /// </summary>
    private void CheckEnrage()
    {
        if (_isEnraged || Kind != MonsterKind.Boss || _maxHP <= 0f)
        {
            return;
        }

        if (_currentHP / _maxHP > enrageHpRatio)
        {
            return;
        }

        _isEnraged = true;
        _attackPower *= enrageMultiplier;

        if (_spriteRenderer != null)
        {
            // 곱하기가 아니라 Lerp로 섞음 - 원래 색이 이미 어둡거나 붉은 계열이어도(예: 오크 보스)
            // 곱하면 차이가 거의 안 보이는데, Lerp면 항상 확실하게 눈에 띄게 바뀜
            _spriteRenderer.color = Color.Lerp(_baseSpriteColor, enrageTintColor, enrageTintStrength);
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
