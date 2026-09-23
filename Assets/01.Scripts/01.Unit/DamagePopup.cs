// 작성자: 김주연
/*
위로 떠오르면서 서서히 사라진 뒤 풀로 되돌아감. 일반 피해/치명타 피해는 색과 크기로 구분함
*/

using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public sealed class DamagePopup : MonoBehaviour, IPoolObject
{
    // 풀 등록/조회
    public const string PoolKey = "DamagePopup";

    // 풀 예열 개수
    private const int POOL_INITIAL_SIZE = 10;

    string IPoolObject.PoolKey => PoolKey;
    int IPoolObject.InitialSize => POOL_INITIAL_SIZE;

    [Header("연결")]
    [Tooltip("숫자를 표시할 TextMeshPro (월드 스페이스)")]
    [SerializeField] private TextMeshPro text;

    [Header("색상")]
    [Tooltip("일반 피해 색상")]
    [SerializeField] private Color normalColor = Color.white;

    [Tooltip("치명타 피해 색상")]
    [SerializeField] private Color criticalColor = new Color(1f, 0.65f, 0f, 1f);

    [Header("크기")]
    [Tooltip("일반 피해일 때 폰트 크기")]
    [SerializeField] private float normalFontSize = 3.5f;

    [Tooltip("치명타 피해일 때 폰트 크기 (더 크게)")]
    [SerializeField] private float criticalFontSize = 5f;

    [Header("애니메이션")]
    [Tooltip("떠오르는 데 걸리는 시간(초)")]
    [SerializeField] private float duration = 0.5f;

    [Tooltip("위로 떠오르는 거리(유닛)")]
    [SerializeField] private float moveDistance = 1.2f;

    [Tooltip("스폰 위치에서 좌우로 흩어지는 정도(유닛). 여러 대상이 동시에 맞아도 숫자가 안 겹치게")]
    [SerializeField] private float randomHorizontalOffset = 0.3f;

    // 풀에서 나올 때마다 새로 발급
    private CancellationTokenSource _cts;

    public void Show(float damage, bool isCritical)
    {
        transform.position += new Vector3(Random.Range(-randomHorizontalOffset, randomHorizontalOffset), 0f, 0f);

        if (text != null)
        {
            text.text = Mathf.RoundToInt(damage).ToString("N0");
            text.color = isCritical ? criticalColor : normalColor;
            text.fontSize = isCritical ? criticalFontSize : normalFontSize;
        }

        PlayAndReturnToPoolAsync(_cts.Token).Forget();
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

    private async UniTaskVoid PlayAndReturnToPoolAsync(CancellationToken token)
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + new Vector3(0f, moveDistance, 0f);
        Color startColor = text != null ? text.color : Color.white;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float ratio = elapsed / duration;
            transform.position = Vector3.Lerp(startPosition, endPosition, ratio);

            if (text != null)
            {
                text.color = new Color(startColor.r, startColor.g, startColor.b, 1f - ratio);
            }

            elapsed += Time.deltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        ObjectPoolManagerTest.Instance.Despawn(PoolKey, this);
    }
}
