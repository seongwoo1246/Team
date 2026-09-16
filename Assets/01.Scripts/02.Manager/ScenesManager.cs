using UnityEngine.SceneManagement;
using Debug = DebugLogger<ScenesManager>;
public enum ScenesName
{
    Login,
    Lobby,
}

public class ScenesManager : Singleton<ScenesManager>
{
    protected override void Awake()
    {
        base.Awake();
    }
    public void LoadScenes(ScenesName name)
    {
        SceneManager.LoadScene((int)name);
    }

    public void StringToLoadScecn(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void LoadScenesAsync(ScenesName name)
    {
        SceneManager.LoadSceneAsync((int)name);
    }
}
