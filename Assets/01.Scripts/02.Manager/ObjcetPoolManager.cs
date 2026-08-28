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
/// ������Ʈ ���� �Ҷ� ����� enum�� ���⿡ ���� ���� �� �ʿ�� ���� ����
/// </summary>
public enum enumType
{
    Cartoon,
    Pixel,
    Item,
    Particle,

}


/// <summary>
/// ��� ������Ʈ Ǯ���� ���õ� �Լ��� ���⼭ �� �ذ� �� Ŭ����
/// </summary>
public class ObjcetPoolManager : MonoBehaviour
{
    public static ObjcetPoolManager Instance {  get; private set; }

    //����� enum�� �־ ����� ���� �ʿ�� ���濹��
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
    /// ���ο� ������ ���� ��Ͽ� �����صδ� �Լ�
    /// </summary>
    /// <param name="enumname">�� Ǯ�� �� enum�� �̸����� Ǯ�� �̸��̱⵵ ��</param>
    /// <param name="prefab">�� ����</param>
    /// <param name="initialSize">�̸� �� ����</param>
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
    /// ���ӿ��� ���� ���� ������ �Լ�
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
    /// �� ����� �� �ǵ����� �Լ�
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
    // ������Ʈ�� ���� ���� �� �־��� ���赵
    private readonly T _prefab;
    // ������Ʈ���� �����ص� �θ� ������Ʈ ��ġ
    private readonly Transform _parent;
    // �ȿ��� �׾Ƶΰ� ���� â���
    private readonly Stack<T> _inactive = new Stack<T>();
    //���� �ۿ��� ����ϰ� ���� ���ǵ� ��Ƶδ� �뵵
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

    // ó�� ���� �� ���� ����� �Լ�
    private T CreateNew()
    {
        return Object.Instantiate(_prefab, _parent);
    }


   /// <summary>
   /// ������Ʈ ������ ����� �� �� �Լ�(������ ���� ������ ���)
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
    /// �ߺ� ������ �� ��� �����ϰ� ��� �� ��Ͽ��� ������ ��Ȱ��ȭ �ϱ�
    /// </summary>
    /// <param name="obj"></param>
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
 ����� ���� ����

1. �����Ҷ��� ������ �������̽� �־��ֱ�
class Ŭ�����̸� : MonoBehaviour , IPoolable
{
    public void OnSpawn()
    {
        ������ �� �� �͵� 
    }
    public void OnDespawn()
    {
        ����� �� �� �͵� 
    }
}



���� ���۽� ����� �� (���� �Ŵ������ ��ŸƮ�� �־���� ��)
ObjcetPoolManager.Instance.RegisterPool(��ȯ�� enum�����ֱ�,��ȯ�� ������, initialSize : ��ȯ�� ����)

  void Start()
    {
        ObjcetPoolManager.Instance.RegisterPool(enumType.Cartoon, monster, 5);
    }

���� �� 
��ȯ�� �� ����Ʈ(Queue)�� ������ ������ ��ȯ, ������ ������ �� ����
Ŭ�����̸� �����̸�  = ObjcetPoolManager.Instance.Spawn<Ŭ�����̸�>("��ȯ�� enum�����ֱ�")

 public void ������ �Լ�()
    {
        test T = ObjcetPoolManager.Instance.Spawn<test>(enumType.Cartoon);
        if(T != null )
        {
            spwan.Add(T);
        }

    }

������� �� 
ObjcetPoolManager.Instance.Despawn("��ȯ�� enum�����ֱ�",�����̸�)

public void ���� �ִ� �Լ�()
    {
        if (����Ʈ(Queue).Count == 0) return;

        int lastIndex = ����Ʈ(Queue).Count - 1;
        test targetObj = ����Ʈ(Queue)[lastIndex];
        
        ObjcetPoolManager.Instance.Despawn(enumType.Cartoon, targetObj);
        spwan.RemoveAt(lastIndex);



    } 

 */