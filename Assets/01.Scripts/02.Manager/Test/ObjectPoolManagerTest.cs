/* 담당자 - 정성우, 송태훈
 
 */
using Cysharp.Threading.Tasks;
using UnityEngine;
using UtilDebug = DebugLogger<ObjectPoolManagerTest>;

public interface IPoolObject : IPoolable
{
    string PoolKey => string.Empty;
    int InitialSize => 1; // 기본 풀 생성 수량 (필요 시 오버라이드)
}
public interface IPool
{
    void Clear();
}

public class ObjectPoolManagerTest : Singleton<ObjectPoolManagerTest>, ILoadable
{
    public int LoadOrder => 5; // AddressableManager(1), DataManager(2) 이후

    // 개별 프리팹 풀 인스턴스 (Key : 프리팹 이름)
    private readonly System.Collections.Generic.Dictionary<string, GameObjectPool> _pools = new();
    private Transform _poolRoot;
    private bool _isInitialized = false;

    protected override void Awake()
    {
        isDDOL = true;
        base.Awake();
        SceneLoadManager.Instance.RegisterLoadable(this);
        EnsureRoot();
    }

    private void EnsureRoot()
    {
        if (_pools != null)
        {
            _poolRoot = new GameObject("Pool_Root").transform;
            _poolRoot.SetParent(transform);
        }
    }

    #region ILoadable 구현 (Addressables Label 자동 풀링)
    public async UniTask OnSceneLoadCreate(SceneId scene)
    {
        if (_isInitialized) return;
        EnsureRoot();
        System.Threading.CancellationToken ct = this.destroyCancellationToken;
        const string poolLabel = "Init_Pool"; // Poolable 컨벤션 : Init_Pool

        UtilDebug.Log($"[{scene}] 라벨('{poolLabel}') 기반 오브젝트 풀 자동 Warmup 시작");

        // 1. 라벨에 해당하는 모든 프리팹 로드
        var prefabs = await AddressableManager.Instance.LoadAssetsByLabelAsync<GameObject>(poolLabel, ct);
        if (prefabs == null || prefabs.Count == 0)
        {
            UtilDebug.Log($"[{scene}] 등록할 풀 에셋이 없습니다. (Label: {poolLabel})");
            return;
        }

        // 2. 프리팹의 IPoolObject 컴포넌트를 탐색하여 자동 풀 등록
        foreach (var prefabGo in prefabs)
        {
            if (prefabGo.TryGetComponent<IPoolObject>(out var poolObj))
            {
                string key = string.IsNullOrEmpty(poolObj.PoolKey) ? prefabGo.name : poolObj.PoolKey;
                RegisterPool(key, prefabGo, poolObj.InitialSize);
            }
            else
            {
                UtilDebug.LogWarning($"프리팹 '{prefabGo.name}'에 IPoolObject 구현체가 없어 풀 등록에서 제외되었습니다.");
            }
        }

        UtilDebug.Log($"[{scene}] 오브젝트 풀 Warmup 완료 (현재 등록된 풀 개수: {_pools.Count})");
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
    private void RegisterPool(string key, GameObject prefab, int initialSize)
    {
        if (_pools.ContainsKey(key))
        {
            UtilDebug.LogWarning($"이미 등록된 풀입니다: {key}");
            return;
        }

        EnsureRoot();

        Transform poolFolder = new GameObject($"Pool_{key}").transform;
        poolFolder.SetParent(_poolRoot);
        _pools[key] = new GameObjectPool(prefab, poolFolder, initialSize);
    }

    public void RegisterPool<T>(string key, GameObject prefabComp, int initialSize) where T : Component, IPoolable
    {
        if (_pools.ContainsKey(key))
        {
            UtilDebug.LogWarning($"이미 등록된 풀입니다: {key}");
            return;
        }

        RegisterPool(key, prefabComp.gameObject, initialSize);
    }
    #endregion

    #region Spawn / Despawn API
    public T Spawn<T>(string key) where T : Component
    {
        if (!_pools.TryGetValue(key, out var pool))
        {
            UtilDebug.LogWarning($"존재하지 않는 풀입니다: {key}");
            return null;
        }

        GameObject go = pool.Get();
        if (go.TryGetComponent<T>(out var comp))
        {
            return comp;
        }

        UtilDebug.LogError($"요청 타입 불일치: {key}은 {typeof(T).Name} 타입 풀이 아닙니다.");
        return null;
    }

    public GameObject Spawn(string key)
    {
        if(!_pools.TryGetValue(key,out var pool))
        {
            UtilDebug.LogError($"존재하지 않는 풀입니다 : {key}");
            return null;
        }
        return pool.Get();
    }

    public void Despawn<T>(string key, T comp) where T : Component, IPoolable
    {
        if (comp == null) return;
        Despawn(key, comp.gameObject);
    }

    public void Despawn(string key, GameObject go)
    {
        if(go == null) return;
        if(_pools.TryGetValue(key, out var pool))
        {
            pool.Return(go);
            return;
        }
        UtilDebug.LogWarning($"반환하려는 풀이 없거나 올바르지 않습니다: {key}");
        Destroy(go);
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

public class GameObjectPool
{
    private readonly GameObject _prefab;
    private readonly Transform _parent;
    private readonly System.Collections.Generic.Stack<GameObject> _inactive = new();

    public GameObjectPool(GameObject prefab, Transform parent, int initialSize)
    {
        _prefab = prefab;
        _parent = parent;
        _inactive = new System.Collections.Generic.Stack<GameObject>(initialSize > 0 ? initialSize : 5);

        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = CreateNew();
            obj.gameObject.SetActive(false);
            _inactive.Push(obj);
        }
    }

    private GameObject CreateNew() => Object.Instantiate(_prefab, _parent);

    public GameObject Get()
    {
        GameObject go = _inactive.Count > 0 ? _inactive.Pop() : CreateNew();
        
        go.SetActive(true);
        if(go.TryGetComponent<IPoolable>(out var poolable))
        {
            poolable.OnSpawn();
        }
        return go;
    }

    public void Return(GameObject go)
    {
        // activeSelf 검사로 중복 반환 원천 차단
        if (!go.gameObject.activeSelf)
        {
            UtilDebug.LogWarning($"중복 반환이거나 비활성화된 오브젝트입니다: {go.name}");
            return;
        }

        if (go.TryGetComponent<IPoolable>(out var poolable))
        {
            poolable.OnDespawn();
        }
        go.gameObject.SetActive(false);
        go.transform.SetParent(_parent);
        _inactive.Push(go);
    }

    public void Clear()
    {
        foreach (GameObject go in _inactive)
        {
            if (go != null) UnityEngine.Object.Destroy(go.gameObject);
        }
        _inactive.Clear();

        if (_parent != null)
        {
            UnityEngine.Object.Destroy(_parent.gameObject);
        }
    }
}