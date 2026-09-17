// 담당자 - 송태훈
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UtilDebug = DebugLogger<TitleBootstrapRunner>;

public class TitleBootstrapRunner : MonoBehaviour
{
    private const string BOOTSTRAP_LOCAL_LABEL = "Bootstrap_Local";

    [SerializeField] private CanvasGroup curtainCanvas;

    private async UniTaskVoid Awake()
    {
        System.Threading.CancellationToken ct = this.destroyCancellationToken;

        // 1. Addressables 초기화
        await Addressables.InitializeAsync().ToUniTask(cancellationToken: ct);

        // 2. Bootstrap_Local 라벨 프리팹 일괄 로드
        var locations = await Addressables.LoadResourceLocationsAsync(BOOTSTRAP_LOCAL_LABEL, typeof(GameObject)).ToUniTask(cancellationToken: ct);
        if (locations == null || locations.Count == 0)
        {
            UtilDebug.LogError($"'{BOOTSTRAP_LOCAL_LABEL}' 라벨을 가진 프리팹을 번들에서 찾을 수 없습니다.");
            return;
        }

        IList<GameObject> prefabs = await Addressables.LoadAssetsAsync<GameObject>(locations,null).ToUniTask(cancellationToken: ct);
         
        TitleBootstrapController bootstrapController = null;

        foreach (var prefab in prefabs)
        {
            var instance = Instantiate(prefab);

            // SceneLoadManager는 생성 즉시 Awake에서 DDOL 처리됨
            if (instance.TryGetComponent<SceneLoadManager>(out _))
            {
                continue;
            }

            // Canvas와 Bootstrap 로직이 묶인 컨트롤러 추출
            if (instance.TryGetComponent<TitleBootstrapController>(out var ctrl) ||
                (ctrl = instance.GetComponentInChildren<TitleBootstrapController>()) != null)
            {
                bootstrapController = ctrl;
            }
        }

        if (bootstrapController != null)
        {
            await FadeOuctCurtainAsync(ct);
            // 3. 프리팹 인스턴스화 완료 후 부트스트랩 시퀀스 가동
            bootstrapController.StartBootstrapSequence();
        }
        else
        {
            UtilDebug.LogError("TitleBootstrapController를 프리팹에서 찾을 수 없습니다.");
        }
    }

    private async UniTask FadeOuctCurtainAsync(System.Threading.CancellationToken ct)
    {
        if (curtainCanvas == null) return;
        float fadeDuration = 0.4f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            curtainCanvas.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        curtainCanvas.alpha = 0f;
        curtainCanvas.blocksRaycasts = false;

        // 페이드 완료 후 캔버스 오브젝트 파괴
        Destroy(curtainCanvas.gameObject);
    }
}