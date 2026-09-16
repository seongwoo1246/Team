/* 담당자 - 송태훈
 각 씬 별 데이터 캐싱 및 조회 총괄 매니저
 */
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UtilDebug = DebugLogger<DataManager>;

public class DataManager : Singleton<DataManager>, ILoadable
{
    public int LoadOrder => 2;
    private readonly Dictionary<System.Type, Dictionary<string, ScriptableObject>> _identifiedDataCache = new();
    private readonly Dictionary<System.Type, ScriptableObject> _singleDataCache = new();

    protected override void Awake()
    {
        isDDOL = true;
        base.Awake();
    }

    public async UniTask OnSceneLoadCreate(SceneId scene)
    {
        if (scene == SceneId.None) return;

        System.Threading.CancellationToken ct = this.destroyCancellationToken;
        UtilDebug.Log($"[{scene}] 데이터 로드 시작");

        // 1. 해당 씬 설정 SO 로드 ( 이름 컨벤션 : "{SceneId}_Data" )
        string dataLabel = $"{scene}_Data";
        var loadedAssets = await AddressableManager.Instance.LoadAssetsByLabelAsync<ScriptableObject>(dataLabel, ct);

        if (loadedAssets != null)
        {
            foreach(var aseet in loadedAssets)
            {
                RegisterAsset(aseet);
            }
            UtilDebug.Log($"[{scene}] 기획 데이터 {loadedAssets.Count}개 캐싱 완료");
        }
        else
        {
            UtilDebug.Log($"[{scene}] 로드할 기획 데이터가 없습니다. (Label: {dataLabel})");
        }
    }

    public void Init(SceneId scene) => UtilDebug.Log($"[{scene}] DataManager 초기화 완료");

    public void OnSceneDestory(SceneId scene)
    {
        // 씬 전환 시 캐시 비우기
        _identifiedDataCache.Clear();
        _singleDataCache.Clear();
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
    /// 사용 예 : DataManager.Instance.GetSingle<GameConfig>();
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