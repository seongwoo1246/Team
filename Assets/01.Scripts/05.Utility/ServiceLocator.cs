/* 담당자 - 송태훈
 */

using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System;
using UtilDebug = DebugLogger;
public interface ILoadable
{
    int LoadOrder { get; }
    UniTask OnSceneLoadCreate(SceneId sncen);
    void Init(SceneId scene);

    void OnSceneDestory(SceneId scene);
}

public enum SceneId
{
    None, BootstrapScene, LobbySceneTest
}

public enum ServiceLifetime
{
    Global, // 싱글톤
    Local   // 로컬
}

public static class ServiceLocator
{
    private class ServiceEntry
    {
        public object Instance { get; set; }
        public ServiceLifetime Lifetime { get; set; }
    }
    private static readonly Dictionary<Type, ServiceEntry> _services = new();

    /// <summary>
    /// 서비스 등록
    /// </summary>
    /// <param name="service"> 등록할 서비스 </param>
    /// <param name="lifetime"> 전역 or 로컬 </param>
    public static void Register<T>(T service, ServiceLifetime lifetime = ServiceLifetime.Global) where T : class
    {
        var type = typeof(T);
        if (_services.ContainsKey(type))
        {
            UtilDebug.LogWarningWithTag("ServiceLocator", $"이미 등록된 서비스");
        }

        _services[type] = new ServiceEntry()
        {
            Instance = service,
            Lifetime = lifetime
        };
    }

    /// <summary>
    /// 서비스 수동 해제
    /// </summary>
    public static void Unregister<T>() where T : class
    {
        _services.Remove(typeof(T));
    }

    /// <summary>
    /// 서비스 조회
    /// </summary>
    public static T Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var entry)
            )
        {
            return entry.Instance as T;
        }

        UtilDebug.LogErrorWithTag("ServiceLocator", $"등록되지 않은 서비스 요청: {typeof(T).Name}");
        return null;
    }

    /// <summary>
    /// 서비스 조회 시도
    /// </summary>
    public static bool TryGet<T>(out T service) where T : class
    {
        if (_services.TryGetValue(typeof(T), out var entry))
        {
            service = entry.Instance as T;
            return true;
        }

        UtilDebug.LogErrorWithTag("ServiceLocator", $"{typeof(T).Name}을 찾을 수 없음");
        service = null;
        return false;
    }

    /// <summary>
    /// 씬 전환 시 호출 : Local 서비스들만 일괄 메모리 해제
    /// </summary>
    public static void ClearSceneLocalServices()
    {
        List<Type> toRemove = new();
        foreach (var pair in _services)
        {
            if (pair.Value.Lifetime == ServiceLifetime.Local)
            {
                toRemove.Add(pair.Key);
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            _services.Remove(toRemove[i]);
        }
    }

    /// <summary>
    /// 전체 서비스 초기화 ( 로그아웃 / 부트스트랩 재진입 시 )
    /// </summary>
    public static void Reset()
    {
        _services.Clear();
    }
}
