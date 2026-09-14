using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UtilDebug = DebugLogger<DataManager>;

public class DataManager : Singleton<DataManager>, ILoadable
{
    public int LoadOrder => 5;


    protected override void Awake()
    {
        isDDOL = true;
        base.Awake();
        ServiceLocator.Register<DataManager>(this);
        SceneLoadManager.instance.RegisterLoadable(this);
    }

    public UniTask OnSceneLoadCreate(SceneId sncen)
    {
        // 해당 씬에 필요한 데이터 비동기 로드 구현
        throw new System.NotImplementedException();
    }

    public void Init(SceneId scene)
    {

    }

    public void OnSceneDestory(SceneId scene)
    {
        // 씬 언로드 시 종속 데이터 정리
    }
}
