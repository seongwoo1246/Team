/* 담당자 - 송태훈
Addressables 기반으로 전역 정적 기획 데이터("Static_Data")를 비동기 일괄 로드 및 캐싱하는 매니저
IIdentifiable 기반 다중 데이터와 GameConfig 등 단일 데이터 조회를 위한 제네릭 접근 API를 제공
 */
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UtilDebug = DebugLogger<DataManager>;

public class DataManager : Singleton<DataManager>, ILoadable
{
    public int LoadOrder => 2;
    // 식별자(ID)가 있는 다중 SO 캐시 
    private readonly Dictionary<System.Type, Dictionary<string, ScriptableObject>> _identifiedDataCache = new();

    // 시스템 당 1개만 존재하는 단일 SO 캐시
    private readonly Dictionary<System.Type, ScriptableObject> _singleDataCache = new();

    // 초기화 플래그
    private bool _isDataLoaded = false;

    protected override void Awake()
    {
        isDDOL = true;
        base.Awake();
        SceneLoadManager.Instance.RegisterLoadable(this);
    }

    #region ILoadable 구현
    /// <summary>
    /// Addressables를 통해 'Static_Data' 라벨의 전역 기획 SO들을 비동기 로드하여 타입별 캐시에 자동 분류 등록
    /// 원래는 각 씬 별로 필요한 SO만 로드할 수 있도록 설계하였으나 씬이 총 2개 뿐이여서 그러하지 못함
    /// </summary>
    public async UniTask OnSceneLoadCreate(SceneId scene)
    {
        // 초기화 1회 진행 시 스킵
        if (_isDataLoaded) return;

        System.Threading.CancellationToken ct = this.destroyCancellationToken;
        const string dataLabel = "Static_Data"; // 정적 SO 이름 컨벤션 : "Static_Data" )

        UtilDebug.Log($"전역 기획 데이터 일괄 로드 시작 (Label: {dataLabel}");

        // 전역 SO 이기 때문에 isGlobal = true / 씬 전환 시 ReleaseSceneAssets에 의해 언로드되지 않음
        var loadedAssets = await AddressableManager.Instance.LoadAssetsByLabelAsync<ScriptableObject>(dataLabel, ct, true);

        if (loadedAssets != null && loadedAssets.Count > 0)
        {
            foreach (var aseet in loadedAssets)
            {
                RegisterAsset(aseet);
            }
            UtilDebug.Log($"전역 기획 데이터 {loadedAssets.Count}개 캐싱 완료");
        }
        else
        {
            UtilDebug.Log($"로드할 기획 데이터가 없습니다. (Label: {dataLabel})");
        }
        _isDataLoaded = true;
    }

    public void Init(SceneId scene) => UtilDebug.Log($"DataManager 초기화 완료");

    public void OnSceneDestory(SceneId scene)
    {  /* 전역 데이터이므로 씬 전환 시 캐시를 비우지 않고 영구 유지 */ }
    #endregion
    
    /// <summary>
    /// 로드된 SO를 IIdentifiable 여부에 따라 자동으로 캐시에 등록
    /// </summary>
    private void RegisterAsset(ScriptableObject asset)
    {
        if (asset == null) return;

        System.Type assetType = asset.GetType();


        if (asset is IIdentifiable identifiable)
        {
            if (!_identifiedDataCache.TryGetValue(assetType, out var dict))
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
    /// <summary>
    /// ID 기반 SO 조회 
    /// </summary>
    public T GetData<T>(string id) where T : ScriptableObject, IIdentifiable
    {
        if (_identifiedDataCache.TryGetValue(typeof(T), out var dict))
        {
            if (dict.TryGetValue(id, out var aseet))
            {
                return aseet as T;
            }
        }

        UtilDebug.LogWarning($"데이터를 찾을 수 없음 : {typeof(T).Name} {id}");
        return null;
    }

    /// <summary>
    /// 단일 데이터 조회 ( GameConfig, StageRosterData )
    /// 사용 예 : DataManager.Instance.GetSingle<GameConfig>();
    /// </summary>
    public T GetSingle<T>() where T : ScriptableObject
    {
        if (_singleDataCache.TryGetValue(typeof(T), out var asset))
        {
            return asset as T;
        }
        UtilDebug.LogWarning($"데이터를 찾을 수 없음 : {typeof(T).Name}");
        return null;
    }

    /// <summary>
    /// 특정 타입의 전체 데이터 원본 맵 조회
    /// </summary>
    public bool TryGetAllData<T>(out Dictionary<string, ScriptableObject> dataMap) where T : ScriptableObject, IIdentifiable
    {
        return _identifiedDataCache.TryGetValue(typeof(T), out dataMap);
    }

    /// <summary>
    /// 특정 타입의 전체 데이터를 지연 평가로 순회
    /// 사용 예 : foreach( var stat in DataManager.Instance.GetAllValues<BaseStatData>())
    /// </summary>
    public IEnumerable<T> GetAllData<T>() where T : ScriptableObject, IIdentifiable
    {
        if(_identifiedDataCache.TryGetValue(typeof(T), out var dict))
        {
            foreach(var asset in dict.Values)
            {
                if(asset is T typedAsset)
                {
                    yield return typedAsset;
                }
            }
        }
    }
    #endregion
}