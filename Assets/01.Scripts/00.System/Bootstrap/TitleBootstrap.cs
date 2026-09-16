public static class TitleBootstrap
{
    public static void InitializeSingletons()
    {
        AddressableManager.Instance.Init();
        var sceneLoader = SceneLoadManager.Instance;
        sceneLoader.Init();

        var dataMgr = DataManager.Instance;
        dataMgr.Init();
        
        var poolMgr = ObjectPoolManagerTest.Instance;
        poolMgr.Init();

        sceneLoader.RegisterLoadable(dataMgr);
        sceneLoader.RegisterLoadable(poolMgr);

        // Non-Mono
        UserManager.Instance.Init();
    }

    public static void RegisterTitleLocalServices(BootstrapView view, LoginController loginCtrl)
    {
        ServiceLocator.Register<BootstrapView>(view, ServiceLifetime.Local);
        ServiceLocator.Register<LoginController>(loginCtrl, ServiceLifetime.Local); 
    }
}
