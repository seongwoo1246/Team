/* 담담자 - 송태훈
 
 
 */
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UtilDebug = DebugLogger<AddressableManager>;

public class AddressableManager : Singleton<AddressableManager>
{
    private readonly Dictionary<string, AsyncOperationHandle> assetHandles = new();
    private readonly Dictionary<GameObject, AsyncOperationHandle> instanceHandles = new();

    protected override void Awake()
    {
        isDDOL = true;
        base.Awake();
    }


    #region 원격 카탈로그 및 다운로드 패치
    /// <summary>
    /// 원격 CDN(Cloudflare) 카탈로그를 확인하고 필요 시 다운로드 할 총 용량을 반환
    /// </summary>
    public async UniTask<long> CheckDownladSizeAsync(string labelOrKey, CancellationToken ct = default)
    {
        // addressables 초기화
        await Addressables.InitializeAsync().ToUniTask(cancellationToken: ct);

        // 카탈로그 업데이트 확인
        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        List<string> catalogToUpdate = await checkHandle.ToUniTask(cancellationToken: ct);
        Addressables.Release(checkHandle);

        if (catalogToUpdate != null && catalogToUpdate.Count > 0)
        {
            var updateHandle = Addressables.UpdateCatalogs(catalogToUpdate, false);
            await updateHandle.ToUniTask(cancellationToken: ct);
            Addressables.Release(updateHandle);
        }

        // 특정 라벨 또는 기본 번들 다운로드 사이즈 확인
        var sizeHandle = Addressables.GetDownloadSizeAsync(labelOrKey);
        long downladSize = await sizeHandle.ToUniTask(cancellationToken: ct);
        Addressables.Release(sizeHandle);

        return downladSize;
    }

    /// <summary>
    /// 대상 에셋 번들을 비동기로 다운로드
    /// </summary>
    public async UniTask<bool> DownloadDependenciesAsync(string labelOrKey, Action<float> onProgress = null ,CancellationToken ct = default)
    {
        var downloadHandle = Addressables.DownloadDependenciesAsync(labelOrKey, false);

        while(!downloadHandle.IsDone)
        {
            onProgress?.Invoke(downloadHandle.PercentComplete);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        bool success = downloadHandle.Status == AsyncOperationStatus.Succeeded;
        Addressables.Release(downloadHandle);
        return success;
    }

    #endregion

    #region 씬 및 프리팹 인스턴스화
    /// <summary>
    /// 등록된 씬을 비동기로 로드 - Login, Lobby
    /// </summary>
    public async UniTask<SceneInstance> LoadSceneAsync(string sceneAddress, LoadSceneMode mode = LoadSceneMode.Single, bool activateOnLoad = true)
    {
        var handle = Addressables.LoadSceneAsync(sceneAddress, mode, activateOnLoad);
        return await handle.ToUniTask();
    }

    /// <summary>
    /// Addressables 프리팹을 인스턴스화하고 핸들을 캐싱
    /// </summary>
    public async UniTask<GameObject> InstantiateAsync(string key, Transform parent = null, CancellationToken ct = default)
    {
        var handle = Addressables.InstantiateAsync(key, parent);
        GameObject result = await handle.ToUniTask(cancellationToken: ct);

        if(handle.Status == AsyncOperationStatus.Succeeded)
        {
            instanceHandles[result] = handle;
            return result;
        }

        UtilDebug.LogWarning($"생성 실패 : {key}");
        return null;
    }

    /// <summary>
    /// 프리팹 / 텍스처 / 오디오 등 메모리 원본 에셋을 로드하고 핸들을 보관
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
    #endregion

    #region 메모리 해제
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

    public void ReleaseAsset(string key)
    {
        if(assetHandles.TryGetValue(key, out var handle))
        {
            Addressables.Release(handle);
            assetHandles.Remove(key);
        }
    }

    /// <summary>
    /// 씬 전환 전 모든 캐시된 핸들 정리
    /// </summary>
    public void ReleaseAll()
    {
        foreach (var handle in instanceHandles.Values)
            Addressables.Release(handle);
        instanceHandles.Clear();

        foreach (var handle in assetHandles.Values)
            Addressables.Release(handle);
        assetHandles.Clear();
    }
    #endregion
}
