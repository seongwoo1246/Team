using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UtilDebug = DebugLogger<DataManager>;

public class DataManager : Singleton<DataManager>, ILoadable
{
    public int LoadOrder => 1;
    private readonly Dictionary<System.Type, Dictionary<string, ScriptableObject>> _identifiedDataCache = new();
    private readonly Dictionary<System.Type, ScriptableObject> _singleDataCache = new();
    private SceneDataConfigSO _currentSceneConfig;

    protected override void Awake()
    {
        isDDOL = true;
        base.Awake();
        ServiceLocator.Register<DataManager>(this);
        SceneLoadManager.Instance.RegisterLoadable(this);
    }

    public async UniTask OnSceneLoadCreate(SceneId scene)
    {
        if (scene == SceneId.None) return;
        
        CancellationToken ct = this.destroyCancellationToken;

        var addressableMgr = ServiceLocator.Get<AddressableManager>();

        // 1. 해당 씬 설정 SO 로드 ( 이름 컨벤션 : "{SceneId}_Config" )
        string configKey = $"{scene}_Config";
        _currentSceneConfig = await addressableMgr.LoadAssetAsync<SceneDataConfigSO>(configKey, ct);
        if (_currentSceneConfig == null) return;

        UtilDebug.Log($"[{scene}] 데이터 로드 시작");

        // 2. Config에 등록되 Addressable Label 순회 로드 ( SO 기반 일괄 로드 )
        foreach (string label in _currentSceneConfig.DataLabels)
        {
            var loadedAssets = await addressableMgr.LoadAssetsByLabelAsync<ScriptableObject>(label, ct);
            if(loadedAssets != null)
            {
                foreach(var asset in loadedAssets)
                {
                    RegisterAsset(asset);
                }
            }
        }

        foreach(string key in _currentSceneConfig.IndividualAssetKeys)
        {
            var asset = await addressableMgr.LoadAssetAsync<ScriptableObject>(key, ct);
            if(asset != null)
            {
                RegisterAsset(asset);
            }
        }
    }

    public void Init(SceneId scene)
    {

    }

    public void OnSceneDestory(SceneId scene)
    {
        // 씬 전환 시 캐시 비우기
        _identifiedDataCache.Clear();
        _singleDataCache.Clear();
        _currentSceneConfig = null;
    }

    /// <summary>
    /// 로드된 SO를 IIdentifiable 여부에 따라 자동으로 캐시에 등록
    /// </summary>
    /// <param name="asset"></param>
    private void RegisterAsset(ScriptableObject asset)
    {
        if(asset == null) return;

        System.Type assetType = asset.GetType();


        if(asset is IIdentifiable identifiable)
        {
            if(!_identifiedDataCache.TryGetValue(assetType, out var dict))
            {
                dict = new Dictionary<string, ScriptableObject>();
                _identifiedDataCache[assetType] = dict;
            }

            dict[identifiable.Id] = asset;
        }
        else
        {
            _singleDataCache[assetType] = asset;
        }
    }

    #region 범용 제네릭 Getter API
    public T GetData<T>(string id) where T : ScriptableObject, IIdentifiable
    {
        if(_identifiedDataCache.TryGetValue(typeof(T), out var dict))
        {
            if(dict.TryGetValue(id, out var aseet))
            {
                return aseet as T;
            }
        }

        UtilDebug.LogWarning($"데이터를 찾을 수 없음 : {typeof(T).Name} {id}");
        return null;
    }

    public Dictionary<string, T> GetAllData<T>() where T : ScriptableObject, IIdentifiable
    {
        var result = new Dictionary<string, T>();

        if(_identifiedDataCache.TryGetValue(typeof (T), out var dict))
        {
            foreach(var key in dict)
            {
                result[key.Key] = key.Value as T;
            }
        }
        return result;
    }

    /// <summary>
    /// 단일 데이터 조회 ( GameConfig, StageRosterData )
    /// 사용 예 : DataManager.instance.GetSingle<GameConfig>();
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetSingle<T>() where T : ScriptableObject
    {
        if (_singleDataCache.TryGetValue(typeof(T), out var asset))
        {
            return asset as T;
        }
        UtilDebug.LogWarning($"데이터를 찾을 수 없음 : {typeof(T).Name}");
        return null;
    }
    #endregion
}
