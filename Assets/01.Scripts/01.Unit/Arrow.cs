// 작성자: 김주연
/*
*/

using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class Arrow : MonoBehaviour
{
    [Header("비행 속도")]
    [SerializeField] private float _moveSpeed = 15f;

    [SerializeField] private float _arriveDistance = 0.05f;

    [SerializeField] private float _maxLifetime = 2f;

    public void Fire(Vector3 targetPosition)
    {
        FlyToTargetAsync(targetPosition, this.GetCancellationTokenOnDestroy()).Forget();
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

        Destroy(gameObject);
    }
}
