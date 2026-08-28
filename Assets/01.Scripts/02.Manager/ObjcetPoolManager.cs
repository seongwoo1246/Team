using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 풀링해서 사용할 오브젝트라면 반드시 이 2가지를 해야하기에 만든 인터페이스 
/// </summary>
public interface IPoolable
{
    
    void OnSpawn();

    void OnDespawn();

}

/// <summary>
/// 오브젝트 폴링 할때 사용할 enum을 여기에 정리 변경 및 필요시 수정 예정
/// </summary>
public enum enumType
{
    Cartoon,
    Pixel,
    Item,
    Particle,

}


/// <summary>
/// 모든 오브젝트 풀링에 관련된 함수는 여기서 다 해결 할 클래스
/// </summary>
public class ObjcetPoolManager : MonoBehaviour
{
    public static ObjcetPoolManager Instance {  get; private set; }

    //현재는 enum을 넣어서 사용중 변경 필요시 변경예정
    private readonly Dictionary<enumType, object> _pool = new Dictionary<enumType, object>();

    private void Awake()
    {
        if(Instance == null)
        {

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
       
        
    }



    /// <summary>
    /// 새로운 종류의 폴을 목록에 저장해두는 함수
    /// </summary>
    /// <param name="enumname">이 풀에 들어갈 enum의 이름으로 풀의 이름이기도 함</param>
    /// <param name="prefab">찍어낼 물건</param>
    /// <param name="initialSize">미리 찍어낼 숫자</param>
    public void RegisterPool<T>(enumType enumname, T prefab, int initialSize = 0) where T : Component, IPoolable
    {
        if (_pool.ContainsKey(enumname))
        {
            Debug.Log("이미 같은 이름의 폴이 있음");
            return;
        }

        Transform PoolParent = new GameObject($"Pool_{enumname}").transform;
        // PoolParent.SetParent(transform);

        _pool[enumname] = new Pool<T>(prefab, PoolParent, initialSize);
    }


    /// <summary>
    /// 게임에서 쓰기 위해 꺼내는 함수
    /// </summary>
    public T Spawn<T>(enumType enumname) where T : Component, IPoolable
    {
        if (!_pool.TryGetValue(enumname, out object poolObj))
        {
            Debug.Log($"{enumname}라는 이름표의 폴링이 없습니다. RegisterPool을 먼저 해야 합니다.");
            return null;
        }
        Pool<T> pool = (Pool<T>)poolObj;
        return pool.Get();
    }


    /// <summary>
    /// 다 사용한 후 되돌리기 함수
    /// </summary>
    public void Despawn<T>(enumType enumname, T obj) where T : Component, IPoolable
    {
        if (!_pool.TryGetValue(enumname, out object poolObj))
        {
            Debug.Log($"{enumname}라는 이름표의 폴링이 없습니다.");
            return;
        }

        Pool<T> pool = (Pool<T>)poolObj;
        pool.Return(obj);
    }



}

class Pool<T> where T : Component , IPoolable
{
    // 프로젝트에 새로 만들 때 넣어줄 설계도
    private readonly T _prefab;
    // 프로젝트에서 정리해둘 부모 오브젝트 위치
    private readonly Transform _parent;
    // 안에서 쌓아두고 있을 창고용
    private readonly Stack<T> _inactive = new Stack<T>();
    //지금 밖에서 사용하고 있을 물건들 담아두는 용도
    private readonly HashSet<T> _active = new HashSet<T>();
   

    public Pool(T prefab, Transform parent, int initialSize)
    {
        _prefab = prefab;
        _parent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            T obj = CreateNew();
            obj.gameObject.SetActive(false);
            _inactive.Push(obj);
        }


    }

    // 처음 만들 때 새로 만드는 함수
    private T CreateNew()
    {
        return Object.Instantiate(_prefab, _parent);
    }


   /// <summary>
   /// 오브젝트 꺼내서 사용할 때 쓸 함수(없으면 생성 있으면 사용)
   /// </summary>
   /// <returns></returns>
    public T Get()
    {
        T obj = _inactive.Count > 0 ? _inactive.Pop() : CreateNew();
        obj.gameObject.SetActive(true);
        _active.Add(obj);
        obj.OnSpawn();
        return obj;
    }



    /// <summary>
    /// 중복 수납이 될 경우 무시하고 사용 중 목록에서 제외후 비활성화 하기
    /// </summary>
    /// <param name="obj"></param>
    public void Return(T obj)
    {
        if(!_active.Contains(obj))
        {
            Debug.Log("중복 수납 되고 있습니다. 무시하겠습니다.");
            return;
        }

        obj.OnDespawn();
        obj.gameObject.SetActive(false);
        _active.Remove(obj);
        _inactive.Push(obj);
    }



   

}

/*
 사용을 위한 예시

1. 폴링할때는 무조건 인터페이스 넣어주기
class 클래스이름 : MonoBehaviour , IPoolable
{
    public void OnSpawn()
    {
        등장할 때 할 것들 
    }
    public void OnDespawn()
    {
        사라질 때 할 것들 
    }
}



게임 시작시 등록할 때 (게임 매니저등에서 스타트에 넣어줘야 함)
ObjcetPoolManager.Instance.RegisterPool(소환할 enum적어주기,소환할 프리팹, initialSize : 소환할 숫자)

  void Start()
    {
        ObjcetPoolManager.Instance.RegisterPool(enumType.Cartoon, monster, 5);
    }

꺼낼 때 
소환할 때 리스트(Queue)에 담아줘야 여러개 소환, 여러개 해제할 때 가능
클래스이름 변수이름  = ObjcetPoolManager.Instance.Spawn<클래스이름>("소환할 enum적어주기")

 public void 꺼내는 함수()
    {
        test T = ObjcetPoolManager.Instance.Spawn<test>(enumType.Cartoon);
        if(T != null )
        {
            spwan.Add(T);
        }

    }

집어넣을 때 
ObjcetPoolManager.Instance.Despawn("소환할 enum적어주기",변수이름)

public void 집어 넣는 함수()
    {
        if (리스트(Queue).Count == 0) return;

        int lastIndex = 리스트(Queue).Count - 1;
        test targetObj = 리스트(Queue)[lastIndex];
        
        ObjcetPoolManager.Instance.Despawn(enumType.Cartoon, targetObj);
        spwan.RemoveAt(lastIndex);



    } 

 */