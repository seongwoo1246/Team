using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    [SerializeField] protected bool isDDOL = false;
    private static T _instance;

    public static T instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<T>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject(typeof(T).Name);
                    _instance = obj.AddComponent<T>();

                    var singleton = _instance as Singleton<T>;

                    if (singleton != null && singleton.isDDOL)
                    {
                        DontDestroyOnLoad(obj);
                    }
                }
            }
            else
            {
                var singleton = _instance as Singleton<T>;
                if (singleton != null && singleton.isDDOL)
                {
                    DontDestroyOnLoad(singleton);
                }
            }

            return _instance;
        }
    }


    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            if (isDDOL)
            {
                DontDestroyOnLoad(this.gameObject);
            }
        }
        else if (_instance != this)
        {
            Destroy(this.gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}