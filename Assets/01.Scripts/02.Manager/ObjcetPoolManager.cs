using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 오브젝트 폴링 할 모든 친구들한태 넣어줄 인터페이스
/// </summary>
public interface IPoolable
{
    
    void OnSpawn();

    void OnDespawn();

}

/// <summary>
///폴링 할때 사용할 키값들 불러올 때 자신이 불러 오는 종류에 맞게 가져가 사용
/// </summary>
public enum enumType
{
    Cartoon,
    Pixel,
    Item,
    Particle,

}


/// <summary>
/// 실질적으로 사용할 오브젝트 폴링 매니저 클래스
/// </summary>
public class ObjcetPoolManager : MonoBehaviour
{
    public static ObjcetPoolManager Instance {  get; private set; }

   
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
   /// 게임 매니저가 가장 먼저 해줘야 하는 내용들 (이걸 안하면 오브젝트 폴링을 사용할 수 없음)
   /// </summary>
   /// <param name="enumname">자신이 불러올 enum 종류</param>
   /// <param name="prefab">자신이 만들 오브젝트</param>
   /// <param name="initialSize">자신이 만들 갯수 </param>
    public void RegisterPool<T>(enumType enumname, T prefab, int initialSize = 0) where T : Component, IPoolable
    {
        if (_pool.ContainsKey(enumname))
        {
            Debug.Log("�̹� ���� �̸��� ���� ����");
            return;
        }

        Transform PoolParent = new GameObject($"Pool_{enumname}").transform;
        // PoolParent.SetParent(transform);

        _pool[enumname] = new Pool<T>(prefab, PoolParent, initialSize);
    }


    /// <summary>
    /// 실질적으로 소환할 때 사용할 함수
    /// </summary>
    public T Spawn<T>(enumType enumname) where T : Component, IPoolable
    {
        if (!_pool.TryGetValue(enumname, out object poolObj))
        {
            Debug.Log($"{enumname}��� �̸�ǥ�� ������ �����ϴ�. RegisterPool�� ���� �ؾ� �մϴ�.");
            return null;
        }
        Pool<T> pool = (Pool<T>)poolObj;
        return pool.Get();
    }


    /// <summary>
    ///실질적으로 비활성화 시킬 때 사용할 함수
    /// </summary>
    public void Despawn<T>(enumType enumname, T obj) where T : Component, IPoolable
    {
        if (!_pool.TryGetValue(enumname, out object poolObj))
        {
            Debug.Log($"{enumname}��� �̸�ǥ�� ������ �����ϴ�.");
            return;
        }

        Pool<T> pool = (Pool<T>)poolObj;
        pool.Return(obj);
    }



}

class Pool<T> where T : Component , IPoolable
{
    // 소환 원본
    private readonly T _prefab;
    //소환 위치
    private readonly Transform _parent;
    // 오브젝트 폴링 담아둘 콜렉션
    private readonly Stack<T> _inactive = new Stack<T>();
    //현재 활동 중인 폴링 소환물들
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

    // 처음에 없을 시 오브젝트 풀링을 만듬
    private T CreateNew()
    {
        return Object.Instantiate(_prefab, _parent);
    }


 
    public T Get()
    {
        T obj = _inactive.Count > 0 ? _inactive.Pop() : CreateNew();
        obj.gameObject.SetActive(true);
        _active.Add(obj);
        obj.OnSpawn();
        return obj;
    }




    public void Return(T obj)
    {
        if(!_active.Contains(obj))
        {
            Debug.Log("�ߺ� ���� �ǰ� �ֽ��ϴ�. �����ϰڽ��ϴ�.");
            return;
        }

        obj.OnDespawn();
        obj.gameObject.SetActive(false);
        _active.Remove(obj);
        _inactive.Push(obj);
    }



   

}

/*
 사용방법 예시

1. 인터페이스 적용
class 클래스 이름 : MonoBehaviour , IPoolable
{
    public void OnSpawn()
    {
        소환할 때 해야 할 일들
    }
    public void OnDespawn()
    {
        비활성화 할 때 할 일들
    }
}



2. RegisterPool을 게임 매니저에서 실행
ObjcetPoolManager.Instance.RegisterPool(소환할 enum종류,소환할 오브젝트, initialSize : 소환할 갯수)

  void Start()
    {
        ObjcetPoolManager.Instance.RegisterPool(enumType.Cartoon, monster, 5);
    }

꺼낼 때
이거를 사용 할 때는 미리 List혹은 Queue, Stack등으로 미리 만들어두고 담아써야 여러개 가능
소환클래스 변수명  = ObjcetPoolManager.Instance.Spawn<소환클래스>("소환할 enum종류")

 public void 소환함수()
    {
        test T = ObjcetPoolManager.Instance.Spawn<test>(enumType.Cartoon);
        if(T != null )
        {
            spwan.Add(T);
        }

    }

집어 넣을 때
ObjcetPoolManager.Instance.Despawn("소환클래스",변수명)

public void 비활성화 함수()
    {
        if (����Ʈ(Queue).Count == 0) return;

        int lastIndex = List(Queue).Count - 1;
        test targetObj = List(Queue)[lastIndex];
        
        ObjcetPoolManager.Instance.Despawn(enumType.Cartoon, targetObj);
        spwan.RemoveAt(lastIndex);



    } 

 */