using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;







/// <summary>
/// 리소스를 번들로 받아와 찾아서 꺼내 쓸때 사용할 클래스
/// </summary>
public class AddressableLoader : MonoBehaviour
{
    //로드된 에셋 과 인스턴스 핸들 관리
    private readonly Dictionary<string, AsyncOperationHandle> _assetHandles = new();
    private readonly Dictionary<GameObject, AsyncOperationHandle> _instanceHandles = new();

    #region 1. 원격 서버 패치 및 카탈로그 업데이트


    /// <summary>
    /// 게임 시작 시 원격 서버의 패치 용량이 충분한지 확인 후 진행 
    /// </summary>
    public async UniTask<bool> CheckAndDownLoadUpdateAsync(Action<float> OnProgress = null)
    {
        // 카탈로그 업데이크 체크
        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        var catalogsToUpdate = await checkHandle.ToUniTask();
        Addressables.Release(checkHandle);

        if(catalogsToUpdate == null|| catalogsToUpdate.Count == 0)
        {
            // 매니저 최신 버전이라고 알려주기
            return true;
        }

        //카탈로그 업데이트 적용
        var updateHandle = Addressables.UpdateCatalogs(catalogsToUpdate,false);
        await updateHandle.ToUniTask();
        Addressables.Release(updateHandle);

        //다운로드 필요한 총 용량 확인
        var sizeHandle = Addressables.GetDownloadSizeAsync(catalogsToUpdate);
        long downloadSize = await sizeHandle.ToUniTask();
        Addressables.Release(sizeHandle);

        if(downloadSize > 0)
        {
            DebugLogger<AddressableLoader>.Log($"Addressable Manager 다운로드 필요 용량 : {downloadSize / (1024f * 1024f): F2}MB");
           


            //에셋 패치 다운로드 진행
            var downloadHandle = Addressables.DownloadDependenciesAsync(catalogsToUpdate, Addressables.MergeMode.Union);

            while(!downloadHandle.IsDone)
            {
                OnProgress?.Invoke(downloadHandle.PercentComplete);
                await UniTask.Yield();
            }
            bool success = downloadHandle.Status == AsyncOperationStatus.Succeeded;
            Addressables.Release(downloadHandle);

            return success;
        }

        return true;


    }

    #endregion


    #region 에셋 로드 및 생성 (Spawn/ Instantiate)


    /// <summary>
    /// 프리팹을 씬에 직접 소환 할 필요가 있을 때 사용 이거는 (ReleaseInstance로 제거해야함)
    /// </summary>
    /// <param name="key">딕셔너리의 키값 프리팹에 이름</param>
    /// <param name="parent">소환할 장소</param>
    /// <returns>성공하면 결과를 집어넣고 실패하면 null처리</returns>
    public async UniTask<GameObject> InstantiateAsync(string key , Transform parent =null)
    {
        var handle = Addressables.InstantiateAsync(key, parent);
        GameObject result = await handle.ToUniTask();

        if( handle.Status == AsyncOperationStatus.Succeeded)
        {
            _instanceHandles[result] = handle;
            return result;
        }
        DebugLogger<AddressableLoader>.Log($"Instantiate 실패했습니다. {key}를 다시 한번 확인 부탁드립니다.");
        return null;
    }


    /// <summary>
    /// 프리팹/오디오/텍스처 등의 에셋원본만 메모리에 로드 할때 사용
    /// </summary>
    /// <typeparam name="T">소환 종류</typeparam>
    /// <param name="key">딕셔너리의 키값</param>
    /// <returns></returns>
    public async UniTask<T> LoadAssetAsync<T> (string key) where T : UnityEngine.Object
    {
        if(_assetHandles.TryGetValue(key, out var existingHandle))
        {
            return (T)existingHandle.Result;
        }

        var handle = Addressables.LoadAssetAsync<T>(key);
        T result = await handle.ToUniTask();

        if(handle.Status == AsyncOperationStatus.Succeeded)
        {
            _assetHandles[key] = handle;
            return result;
        }
        DebugLogger<AddressableLoader>.Log($"LoadAsset 실패 {key}");
        return null;
    }

    #endregion

    #region 메모리 해제(Release)

    /// <summary>
    /// InstanfiatAsync로 생성한 게임 오브젝트는 이걸로 파괴 및 메모리 해제
    /// </summary>
    /// <param name="Go"></param>
    public void ReleaseInstance(GameObject Go)
    {
        if (Go == null) return;

        if(_instanceHandles.TryGetValue(Go,out var handle))
        {
            Addressables.ReleaseInstance(Go);
            _instanceHandles.Remove(Go);
        }
        else
        {
            Destroy(Go);
        }
    }

    /// <summary>
    /// LoadAssetAsync로 로드한 메모리 에셋을 해제하는 함수
    /// </summary>
    /// <param name="key">해제할 에셋이름</param>
    public void ReleaseAsset(string key)
    {
        if( _assetHandles.TryGetValue(key,out var handle))
        {
            Addressables.Release(handle);
            _assetHandles.Remove(key);
        }
    }

    #endregion
}

/*
만들었으면 해제까지 한 세트로 진행 되게 만들기 
Group 관련 해서 주의해서 만들기 로비와 로그인으로 1차적으로 나눌 예정

 */
