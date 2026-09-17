/* 담당자 - 정성우, 송태훈
 
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UtilDebug = DebugLogger<ObjectPoolManagerTest>;

public interface IPoolObject : IPoolable
{
    enumType PoolType { get; }
    int InitialSize => 1; // 기본 풀 생성 수량 (필요 시 오버라이드)
}
public interface IPool
{
    void Clear();
}

public class ObjectPoolManagerTest : Singleton<ObjectPoolManagerTest>, ILoadable
{
    public int LoadOrder => 5; // AddressableManager(1), DataManager(2) 이후

    private readonly Dictionary<enumType, IPool> _pools = new();
    private Transform _poolRoot;
    private bool _isInitialized = false;

    protected override void Awake()
    {
        isDDOL = true;
        base.Awake();
        EnsureRoot();
    }

    private void EnsureRoot()
    {
        if (_pools == null)
        {
            _poolRoot = new GameObject("Pool_Root").transform;
            _poolRoot.SetParent(transform);
        }
    }

    #region ILoadable 구현 (Addressables Label 자동 풀링)
    public async UniTask OnSceneLoadCreate(SceneId scene)
    {
        //if (_isInitialized) return;
        //EnsureRoot();
        //System.Threading.CancellationToken ct = this.destroyCancellationToken;
        //const string poolLabel = "Init_Pool"; // Poolable 컨벤션 : Init_Pool

        //UtilDebug.Log($"[{scene}] 라벨('{poolLabel}') 기반 오브젝트 풀 자동 Warmup 시작");

        //// 1. 라벨에 해당하는 모든 프리팹 로드
        //var prefabs = await AddressableManager.Instance.LoadAssetsByLabelAsync<GameObject>(poolLabel, ct);
        //if (prefabs == null || prefabs.Count == 0)
        //{
        //    UtilDebug.Log($"[{scene}] 등록할 풀 에셋이 없습니다. (Label: {poolLabel})");
        //    return;
        //}

        //// 2. 프리팹의 IPoolObject 컴포넌트를 탐색하여 자동 풀 등록
        //foreach (var prefabGo in prefabs)
        //{
        //    if (prefabGo.TryGetComponent<IPoolObject>(out var poolObj))
        //    {
        //        RegisterPool((Component)poolObj, poolObj.PoolType, poolObj.InitialSize);
        //    }
        //    else
        //    {
        //        UtilDebug.LogWarning($"프리팹 '{prefabGo.name}'에 IPoolObject 구현체가 없어 풀 등록에서 제외되었습니다.");
        //    }
        //}

        //UtilDebug.Log($"[{scene}] 오브젝트 풀 Warmup 완료 (현재 등록된 풀 개수: {_pools.Count})");
        await UniTask.Yield();
    }

    public void Init(SceneId scene)
    {
        if (_isInitialized) return;
        _isInitialized = true;
        UtilDebug.Log($"[{scene}] ObjectPoolManagerTest 초기화 완료");
    }

    public void OnSceneDestory(SceneId scene)
    {   /* 모든 풀은 계속 전역으로 유지되므로 씬 전환 시 파괴하지 않음 */ }
    #endregion

    #region 풀 등록 (내부 및 수동 등록 API)
    private void RegisterPool(Component prefabComp, enumType type, int initialSize)
    {
        if (_pools.ContainsKey(type))
        {
            UtilDebug.LogWarning($"이미 등록된 풀입니다: {type}");
            return;
        }

        EnsureRoot();
        Transform poolFolder = new GameObject($"Pool_{type}").transform;
        poolFolder.SetParent(_poolRoot);

        // 리플렉션 없이 타입 안전한 풀 인스턴스 동적 생성
        System.Type poolType = typeof(Pool<>).MakeGenericType(prefabComp.GetType());
        IPool poolInstance = (IPool)System.Activator.CreateInstance(poolType, prefabComp, poolFolder, initialSize);

        _pools[type] = poolInstance;
    }

    public void RegisterPool<T>(enumType type, T prefab, int initialSize = 0, bool isGlobal = false) where T : Component, IPoolable
    {
        if (_pools.ContainsKey(type))
        {
            UtilDebug.LogWarning($"이미 등록된 풀입니다: {type}");
            return;
        }

        EnsureRoot();
        Transform poolFolder = new GameObject($"Pool_{type}").transform;
        poolFolder.SetParent(_poolRoot);

        _pools[type] = new CompPool<T>(prefab, poolFolder, initialSize, isGlobal);
    }
    #endregion

    #region Spawn / Despawn API
    public T Spawn<T>(enumType type) where T : Component, IPoolable
    {
        if (!_pools.TryGetValue(type, out var poolObj))
        {
            UtilDebug.LogWarning($"존재하지 않는 풀입니다: {type}");
            return null;
        }

        if (poolObj is CompPool<T> pool)
        {
            return pool.Get();
        }

        UtilDebug.LogError($"[ObjectPoolManager] 요청 타입 불일치: {type}은 {typeof(T).Name} 타입 풀이 아닙니다.");
        return null;
    }

    public void Despawn<T>(enumType type, T obj) where T : Component, IPoolable
    {
        if (obj == null) return;

        if (!_pools.TryGetValue(type, out var poolObj))
        {
            UtilDebug.LogWarning($"오브젝트를 되돌릴 풀이 없습니다: {type}");
            Destroy(obj.gameObject);
            return;
        }

        if (poolObj is CompPool<T> pool)
        {
            pool.Return(obj);
            return;
        }

        UtilDebug.LogWarning($"반환하려는 풀이 없거나 올바르지 않습니다: {type}");
        Destroy(obj.gameObject);
    }
    #endregion


    /// <summary>
    /// 게임 종료 시 전체 풀 메모리 해제
    /// </summary>
    private void OnDestroy()
    {
        foreach (var pool in _pools.Values)
        {
            pool.Clear();
        }
        _pools.Clear();
    }
}

public class CompPool<T> : IPool where T : Component, IPoolable
{
    private readonly T _prefab;
    private readonly Transform _parent;
    private readonly Stack<T> _inactive = new();

    public CompPool(T prefab, Transform parent, int initialSize, bool isGlobal = false)
    {
        _prefab = prefab;
        _parent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            T obj = CreateNew();
            obj.gameObject.SetActive(false);
            _inactive.Push(obj);
        }
    }

    private T CreateNew() => Object.Instantiate(_prefab, _parent);

    public T Get()
    {
        T obj = _inactive.Count > 0 ? _inactive.Pop() : CreateNew();
        obj.gameObject.SetActive(true);
        obj.OnSpawn();
        return obj;
    }

    public void Return(T obj)
    {
        // activeSelf 검사로 중복 반환 원천 차단
        if (!obj.gameObject.activeSelf)
        {
            UtilDebug.LogWarning($"중복 반환이거나 비활성화된 오브젝트입니다: {obj.name}");
            return;
        }

        obj.OnDespawn();
        obj.gameObject.SetActive(false);
        obj.transform.SetParent(_parent);
        _inactive.Push(obj);
    }

    public void Clear()
    {
        foreach (var obj in _inactive)
        {
            if (obj != null) UnityEngine.Object.Destroy(obj.gameObject);
        }
        _inactive.Clear();

        if (_parent != null)
        {
            UnityEngine.Object.Destroy(_parent.gameObject);
        }
    }
}