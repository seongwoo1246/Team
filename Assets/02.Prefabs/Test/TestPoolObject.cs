using UnityEngine;

public class TestPoolObject : MonoBehaviour, IPoolable
{
    public void OnSpawn()
    {
        Debug.Log($"[TestPoolObject] {gameObject.name} 스폰 (OnSpawn 호출됨)");
    }

    public void OnDespawn()
    {
        Debug.Log($"[TestPoolObject] {gameObject.name} 디스폰 (OnDespawn 호출됨)");
    }
}