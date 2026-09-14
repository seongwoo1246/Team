using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UtilDebug = DebugLogger<ObjectPoolManager>;

public interface IPool
{
    bool IsGlobal { get; }
    void Clear();
}

public class ObjectPoolManager : Singleton<ObjectPoolManager>, ILoadable
{
    public int LoadOrder => 5; // DataManager(1) 이후, UIManager(10) 이전

    private readonly Dictionary<enumType, IPool> _pools = new();
    private Transform _globalRoot;
    private Transform _sceneLocalRoot;

    protected override void Awake()
    {
        isDDOL = true;
        base.Awake();
        ServiceLocator.Register<ObjectPoolManager>(this);
        SceneLoadManager.Instance.RegisterLoadable(this);

        InitRoots();
    }

    private void InitRoots()
    {
        if (_globalRoot == null)
        {
            _globalRoot = new GameObject("[Pool_Global]").transform;
            _globalRoot.SetParent(transform);
        }

        if (_sceneLocalRoot == null)
        {
            _sceneLocalRoot = new GameObject("[Pool_SceneLocal]").transform;
            _sceneLocalRoot.SetParent(transform);
        }
    }

    #region ILoadable 구현
    public async UniTask OnSceneLoadCreate(SceneId scene)
    {
        // 씬 전환 후 로컬 루트 복구
        if (_sceneLocalRoot == null)
        {
            _sceneLocalRoot = new GameObject("[Pool_SceneLocal]").transform;
            _sceneLocalRoot.SetParent(transform);
        }

        if (scene == SceneId.None || scene == SceneId.BootstrapScene)
            return;

        var config = DataManager.Instance.GetSingle<SceneDataConfigSO>();
        if (config == null || config.ScenePoolList == null || config.ScenePoolList.Count == 0)
        {
            UtilDebug.Log($"[{scene}] 등록할 ScenePoolList가 없습니다.");
            return;
        }

        var ct = this.destroyCancellationToken;
        UtilDebug.Log($"[{scene}] 씬 전용 오브젝트 풀 Warmup 시작 (항목 수: {config.ScenePoolList.Count})");

        foreach (var poolInfo in config.ScenePoolList)
        {
            await RegisterPoolAsync<Component>(
                poolInfo.poolType,
                poolInfo.addressableKey,
                poolInfo.initialCount,
                poolInfo.isGlobal,
                ct
                );
        }
        Debug.Log($"[{scene}] 씬 전용 오브젝트 풀 Warmup 완료");
    }

    public void Init(SceneId scene)
    {
        UtilDebug.Log($"[{scene}] ObjectPoolManager 초기화 완료 (현재 풀 개수: {_pools.Count})");
    }

    public void OnSceneDestory(SceneId scene)
    {
        // 씬 로컬 풀 일괄 정리
        List<enumType> keysToRemove = new();

        foreach (var pair in _pools)
        {
            if (!pair.Value.IsGlobal)
            {
                pair.Value.Clear();
                keysToRemove.Add(pair.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _pools.Remove(key);
        }

        if (_sceneLocalRoot != null)
        {
            Destroy(_sceneLocalRoot.gameObject);
            _sceneLocalRoot = null;
        }

        UtilDebug.Log($"[{scene}] 씬 로컬 오브젝트 풀 정리 완료");
    }
    #endregion

    #region 풀 등록 API
    /// <summary>
    /// Addressable 키를 통해 프리팹을 비동기 로드하여 풀을 생성
    /// </summary>
    public async UniTask RegisterPoolAsync<T>(enumType poolType, string addressableKey, int initialSize = 0, bool isGlobal = false, CancellationToken ct = default) where T : Component
    {
        if (_pools.ContainsKey(poolType))
        {
            UtilDebug.LogWarning($"이미 등록된 풀입니다: {poolType}");
            return;
        }

        var addressableMgr = ServiceLocator.Get<AddressableManager>();
        T prefab = await addressableMgr.LoadPrefabComponentAsync<T>(addressableKey, ct);

        if (prefab == null)
        {
            UtilDebug.LogError($"풀 생성 실패: Addressable 키 '{addressableKey}'에서 {typeof(T).Name} 프리팹을 찾지 못했습니다.");
            return;
        }

        RegisterPool(poolType, prefab, initialSize, isGlobal);
    }

    /// <summary>
    /// 기존 프리팹 컴포넌트 직접 등록
    /// </summary>
    public void RegisterPool<T>(enumType poolType, T prefab, int initialSize = 0, bool isGlobal = false) where T : Component
    {
        if (_pools.ContainsKey(poolType))
        {
            UtilDebug.LogWarning($"이미 등록된 풀입니다: {poolType}");
            return;
        }

        Transform parentRoot = isGlobal ? _globalRoot : _sceneLocalRoot;
        Transform poolFolder = new GameObject($"Pool_{poolType}").transform;
        poolFolder.SetParent(parentRoot);

        _pools[poolType] = new ComponentPool<T>(prefab, poolFolder, initialSize, isGlobal);
    }
    #endregion

    #region Spawn / Despawn API
    public T Spawn<T>(enumType poolType, Vector3 position = default, Quaternion rotation = default) where T : Component
    {
        if (!_pools.TryGetValue(poolType, out var poolObj))
        {
            UtilDebug.LogWarning($"존재하지 않는 풀입니다: {poolType}");
            return null;
        }

        if (poolObj is ComponentPool<T> pool)
        {
            return pool.Get(position, rotation);
        }

        UtilDebug.LogError($"풀 타입 불일치: {poolType}은 {typeof(T).Name} 타입이 아닙니다.");
        return null;
    }

    public void Despawn<T>(enumType poolType, T obj) where T : Component
    {
        if (obj == null) return;

        if (!_pools.TryGetValue(poolType, out var poolObj))
        {
            UtilDebug.LogWarning($"반환하려는 풀이 없습니다: {poolType}");
            Destroy(obj.gameObject);
            return;
        }

        if (poolObj is ComponentPool<T> pool)
        {
            pool.Return(obj);
        }
    }
    #endregion
}

public class ComponentPool<T> : IPool where T : Component
{
    private readonly T _prefab;
    private readonly Transform _parent;
    private readonly bool _isGlobal;
    private readonly Stack<T> _inactive = new();
    private readonly HashSet<T> _active = new();

    public bool IsGlobal => _isGlobal;

    public ComponentPool(T prefab, Transform parent, int initialSize, bool isGlobal)
    {
        _prefab = prefab;
        _parent = parent;
        _isGlobal = isGlobal;

        for (int i = 0; i < initialSize; i++)
        {
            T obj = CreateNew();
            obj.gameObject.SetActive(false);
            _inactive.Push(obj);
        }
    }

    private T CreateNew()
    {
        return UnityEngine.Object.Instantiate(_prefab, _parent);
    }

    public T Get(Vector3 position, Quaternion rotation)
    {
        T obj = _inactive.Count > 0 ? _inactive.Pop() : CreateNew();

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.gameObject.SetActive(true);
        _active.Add(obj);
        if(obj.TryGetComponent<IPoolable>(out var poolable))
        {
            poolable.OnSpawn();
        }
        return obj;
    }

    public void Return(T obj)
    {
        if (!_active.Contains(obj))
        {
            UtilDebug.LogWarning($"중복 반환이거나 활성화되지 않은 오브젝트입니다: {obj.name}");
            return;
        }

        if(obj.TryGetComponent<IPoolable>(out var poolable))
        {
            poolable.OnDespawn();
        }
        obj.gameObject.SetActive(false);
        obj.transform.SetParent(_parent);
        _active.Remove(obj);
        _inactive.Push(obj);
    }

    public void Clear()
    {
        foreach (var obj in _active)
        {
            if (obj != null) UnityEngine.Object.Destroy(obj.gameObject);
        }
        foreach (var obj in _inactive)
        {
            if (obj != null) UnityEngine.Object.Destroy(obj.gameObject);
        }

        _active.Clear();
        _inactive.Clear();

        if (_parent != null)
        {
            UnityEngine.Object.Destroy(_parent.gameObject);
        }
    }
}