using UnityEngine;

public class PoolTestController : MonoBehaviour
{
    private TestPoolObject spawnedObj;

    private void Update()
    {
        // 스페이스바를 누르면 풀에서 꺼냄
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var poolMgr = ServiceLocator.Get<ObjectPoolManager>();
            spawnedObj = poolMgr.Spawn<TestPoolObject>(enumType.Cartoon_Monster, Vector3.zero, Quaternion.identity);
            Debug.Log($"스폰 성공: {spawnedObj != null}");
        }

        // R 키를 누르면 풀로 반환
        if (Input.GetKeyDown(KeyCode.R) && spawnedObj != null)
        {
            var poolMgr = ServiceLocator.Get<ObjectPoolManager>();
            poolMgr.Despawn(enumType.Cartoon_Monster, spawnedObj);
            spawnedObj = null;
        }
    }
}