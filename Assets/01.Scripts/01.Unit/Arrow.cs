// 작성자: 김주연
/*
궁수가 쏘는 화살 발사체
*/

using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class Arrow : MonoBehaviour, IPoolObject
{
    public const string PoolKey = "Arrow";

    // 풀 예열 개수
    private const int POOL_INITIAL_SIZE = 10;

    string IPoolObject.PoolKey => PoolKey;
    int IPoolObject.InitialSize => POOL_INITIAL_SIZE;

    [Header("비행 속도")]
    [SerializeField] private float _moveSpeed = 15f;

    [SerializeField] private float _arriveDistance = 0.05f;

    [SerializeField] private float _maxLifetime = 2f;

    private CancellationTokenSource _cts;

    public void Fire(Vector3 targetPosition)
    {
        FlyToTargetAsync(targetPosition, _cts.Token).Forget();
    }

    public void OnSpawn()
    {
        _cts = new CancellationTokenSource();
    }

    public void OnDespawn()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async UniTaskVoid FlyToTargetAsync(Vector3 targetPosition, CancellationToken token)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        float elapsed = 0f;
        while (elapsed < _maxLifetime)
        {
            if (Vector3.Distance(transform.position, targetPosition) <= _arriveDistance)
            {
                break;
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, _moveSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        ObjectPoolManagerTest.Instance.Despawn(PoolKey, this);
    }
}
