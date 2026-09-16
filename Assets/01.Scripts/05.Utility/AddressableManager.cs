/* 담담자 - 송태훈
 Cloudflare CDN 연동 카탈로그/패치 관리 및 Addresaable 에셋 수명 주기 총괄 매니저
 */
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UtilDebug = DebugLogger<AddressableManager>;

public class AddressableManager : Singleton<AddressableManager>
{
    // 로드된 에셋 핸들 캐시 ( 프리팹 원본, 텍스처, SO 등 )
    private readonly Dictionary<string, AsyncOperationHandle> assetHandles = new();
    // 런타임에 Instantiate된 오브젝트 핸들 캐시 ( 풀링되지 않는 1회성 오브젝트용)
    private readonly Dictionary<GameObject, AsyncOperationHandle> instanceHandles = new();
    private List<IResourceLocation> _downloadLocations = new();
    public int LoadOrder => 1;

    protected override void Awake()
    {
        isDDOL = true;
        base.Awake();
    }

    #region 원격 카탈로그 및 CDN 다운로드 패치
    /// <summary>
    /// 원격 CDN(Cloudflare) 카탈로그를 확인하고 모든 원격 에셋의 다운로드 필요 총 용량을 산출
    /// </summary>
    public async UniTask<long> CheckTotalDownloadSizeAsync(CancellationToken ct = default)
    {
        // addressables 초기화
        await Addressables.InitializeAsync().ToUniTask(cancellationToken: ct);

        // 카탈로그 업데이트 확인
        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        List<string> catalogToUpdate = await checkHandle.ToUniTask(cancellationToken: ct);
        Addressables.Release(checkHandle);

        if (catalogToUpdate != null && catalogToUpdate.Count > 0)
        {
            UtilDebug.Log($"Cloudflare 새 카탈로그 발견 : {catalogToUpdate.Count}개 업데이트");

            var updateHandle = Addressables.UpdateCatalogs(catalogToUpdate, false);
            await updateHandle.ToUniTask(cancellationToken: ct);
            Addressables.Release(updateHandle);
        }

        // 다운로드 용량 산출
        var allkeys = new HashSet<object>();
        foreach(IResourceLocator locator in Addressables.ResourceLocators)
        {
            foreach(object key in locator.Keys)
            {
                allkeys.Add(key);
            }
        }
        var locationsHandle = Addressables.LoadResourceLocationsAsync(allkeys, Addressables.MergeMode.Union);
        IList<IResourceLocation> locations = await locationsHandle.ToUniTask(cancellationToken: ct);

        _downloadLocations = new List<IResourceLocation>(locations);
        Addressables.Release(locationsHandle);

        if (_downloadLocations.Count == 0)
            return 0;

        var sizeHandle = Addressables.GetDownloadSizeAsync(_downloadLocations);
        long downloadSize = await sizeHandle.ToUniTask(cancellationToken: ct);
        Addressables.Release(sizeHandle);

        return downloadSize;
    }

    /// <summary>
    /// CheckTotalDownloadSizeAsync에서 감지된 모든 원격 의존성 에셋 번들 일괄 다운로드
    /// </summary>
    public async UniTask<bool> DownloadAllDependenciesAsync(Action<float> onProgress = null ,CancellationToken ct = default)
    {
        if (_downloadLocations == null || _downloadLocations.Count == 0)
            return true;

        var downloadHandle = Addressables.DownloadDependenciesAsync(_downloadLocations, false);

        while(!downloadHandle.IsDone)
        {
            onProgress?.Invoke(downloadHandle.PercentComplete);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        bool success = downloadHandle.Status == AsyncOperationStatus.Succeeded;
        Addressables.Release(downloadHandle);

        // 다운로드 완료 후 로케이션 캐시 정리
        _downloadLocations.Clear();
        return success;
    }

    #endregion

    #region 씬 비동기 로드
    /// <summary>
    /// 등록된 씬을 비동기로 로드 - Login, Lobby
    /// </summary>
    public async UniTask<SceneInstance> LoadSceneAsync(string sceneAddress, LoadSceneMode mode = LoadSceneMode.Single, bool activateOnLoad = true)
    {
        var handle = Addressables.LoadSceneAsync(sceneAddress, mode, activateOnLoad);
        return await handle.ToUniTask();
    }
    #endregion

    #region ObjectPool 연동을 위한 프리팹 및 에셋 로드
    /// <summary>
    /// 프리팹 / 텍스처 / 오디오 등 메모리 원본 단일 에셋을 로드하고 핸들을 보관
    /// </summary>
    public async UniTask<T> LoadAssetAsync<T>(string key, CancellationToken ct = default) where T : UnityEngine.Object
    {
        if(assetHandles.TryGetValue(key, out var existingHandle))
            return (T)existingHandle.Result;

        var handle = Addressables.LoadAssetAsync<T>(key);
        T result = await handle.ToUniTask(cancellationToken: ct);

        if(handle.Status == AsyncOperationStatus.Succeeded)
        {
            assetHandles[key] = handle;
            return result;
        }

        UtilDebug.LogWarning($"에셋 로드 실패 : {key}");
        return null;
    }

    public async UniTask<IList<T>> LoadAssetsByLabelAsync<T>(string label, CancellationToken ct = default, Action<T> callback = null) where T : UnityEngine.Object
    {
        var handle = Addressables.LoadAssetsAsync<T>(label, callback);
        var result = await handle.ToUniTask(cancellationToken: ct);

        if(handle.Status == AsyncOperationStatus.Succeeded)
        {
            assetHandles[label] = handle;
            return result;
        }

        UtilDebug.LogWarning($"레이블 로드 실패 : {label}");
        return null;
    }

    public async UniTask<T> LoadPrefabComponentAsync<T>(string key, CancellationToken ct = default)
    {
        GameObject go = await LoadAssetAsync<GameObject>(key, ct);
        if (go != null && go.TryGetComponent<T>(out var comp))
            return comp;

        UtilDebug.LogError($"프리팹 {key}에서 컴포넌트({typeof(T).Name})을 찾을 수 없음");
        return default;
    }
    #endregion

    #region 1회성 인스턴스화 ( 풀링 미사용 프리팹 )
    /// <summary>
    /// 풀링하지 않는 1회성 프리팹(예: 고유 팝업 UI)을 Addressables로 바로 생성
    /// </summary>
    public async UniTask<GameObject> InstantiateAsync(string key, Transform parent = null, CancellationToken ct = default)
    {
        var handle = Addressables.InstantiateAsync(key, parent);
        GameObject result = await handle.ToUniTask(cancellationToken: ct);

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            instanceHandles[result] = handle;
            return result;
        }

        UtilDebug.LogWarning($"생성 실패 : {key}");
        return null;
    }
    #endregion

    #region 메모리 해제
    /// <summary>
    /// 특정 인스턴스 해제
    /// </summary>
    /// <param name="go"></param>
    public void ReleaseInstance(GameObject go)
    {
        if (go == null) return;
        if (instanceHandles.TryGetValue(go, out var handle))
        {
            Addressables.ReleaseInstance(go);
            instanceHandles.Remove(go);
        }
        else
            Destroy(go);
    }

    /// <summary>
    /// 씬 전환 시 1회성으로 생성한 인스턴스 모두 해제
    /// </summary>
    public void ReleaseAllInstance()
    {
        foreach(var handle in instanceHandles.Values)
        {
            Addressables.Release(handle);
        }
        instanceHandles.Clear();
    }

    /// <summary>
    /// 에셋을 내림
    /// </summary>
    /// <param name="key"></param>
    public void ReleaseAsset(string key)
    {
        if(assetHandles.TryGetValue(key, out var handle))
        {
            Addressables.Release(handle);
            assetHandles.Remove(key);
        }
    }

    /// <summary>
    /// 캐시된 모든 원본 에셋과 인스턴스 핸들을 강제 정리
    /// </summary>
    public void ReleaseAll()
    {
        ReleaseAllInstance();

        foreach (var handle in assetHandles.Values)
            Addressables.Release(handle);
        assetHandles.Clear();
    }
    #endregion
}
