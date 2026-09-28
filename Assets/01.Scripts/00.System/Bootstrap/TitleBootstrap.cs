/* 담당자 - 송태훈
게임 시작 시 필수 인게임 싱글톤 매니저들을 사전 인스턴화 및 초기화 진행
 */
public static class TitleBootstrap
{
    public static void InitializeSingletons()
    {
        AddressableManager.Instance.Init();

        SoundManager soundManager = SoundManager.Instance;

        SceneLoadManager sceneMng = SceneLoadManager.Instance;

        DataManager dataMng = DataManager.Instance;

        AddressPoolManager ObjectMng = AddressPoolManager.Instance;

        GoldWallet goldWallet = GoldWallet.Instance;

        MaterialWallet materialWallet = MaterialWallet.Instance;

        PlayerLevelSystem playerLevelSystem = PlayerLevelSystem.Instance;

        // Non-Mono
        UserManager.Instance.Init();
    }

    public static void RegisterTitleLocalServices(BootstrapView view, LoginController loginCtrl)
    {
        ServiceLocator.Register<BootstrapView>(view, ServiceLifetime.Local);
        ServiceLocator.Register<LoginController>(loginCtrl, ServiceLifetime.Local); 
    }
}
