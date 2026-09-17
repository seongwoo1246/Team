// 담당자 - 송태훈
using UnityEngine;

public static class TitleBootstrap
{
    public static void InitializeSingletons()
    {
        AddressableManager.Instance.Init();
        
        var s = SceneLoadManager.Instance;

        var d = DataManager.Instance;
        
        var o = ObjectPoolManagerTest.Instance;

        var g = GoldWallet.Instance;

        // Non-Mono
        UserManager.Instance.Init();
    }

    public static void RegisterTitleLocalServices(BootstrapView view, LoginController loginCtrl)
    {
        ServiceLocator.Register<BootstrapView>(view, ServiceLifetime.Local);
        ServiceLocator.Register<LoginController>(loginCtrl, ServiceLifetime.Local); 
    }
}
