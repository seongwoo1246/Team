// 담당자 - 송태훈
using UnityEngine;

public static class TitleBootstrap
{
    public static void InitializeSingletons()
    {
        AddressableManager.Instance.Init();

        SceneLoadManager sceneMng = SceneLoadManager.Instance;

        DataManager dataMng = DataManager.Instance;

        ObjectPoolManagerTest ObjectMng = ObjectPoolManagerTest.Instance;

        GoldWallet goldWallet = GoldWallet.Instance;

        MaterialWallet materialWallet = MaterialWallet.Instance;

        // Non-Mono
        UserManager.Instance.Init();
    }

    public static void RegisterTitleLocalServices(BootstrapView view, LoginController loginCtrl)
    {
        ServiceLocator.Register<BootstrapView>(view, ServiceLifetime.Local);
        ServiceLocator.Register<LoginController>(loginCtrl, ServiceLifetime.Local); 
    }
}
