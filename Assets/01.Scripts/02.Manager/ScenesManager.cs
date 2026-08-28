using UnityEngine.SceneManagement;



public enum ScenesName
{
    Login,
    Lobby,

}





public class ScenesManager : Singleton<ScenesManager>
{
  
    public void LoadScenes(ScenesName name)
    {
        SceneManager.LoadScene((int)name);

    }


}
