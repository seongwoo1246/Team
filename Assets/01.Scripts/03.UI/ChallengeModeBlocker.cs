// 작성자: 김주연
/*
챌린지 모드 중엔 골드 확인/강화를 못하게 막는 스크립트. blockerRoot(까만 이미지)를
챌린지 모드일 때만 켜서 그밑에 있는 Party_Panel(골드 텍스트 + 강화 카드들)을
시각적으로 가리는 동시에 클릭도(raycastTarget) 막는다

StageManager.ModeChanged를 구독해서 모드가 바뀔 때만 blockerRoot를 켜고 끈다
(매프레임 폴링하던걸 이벤트 방식으로 바꿈)
*/

using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// StageManager.CurrentMode를 봐서 blockerRoot를 챌린지 모드일 때만 활성화한다
/// </summary>
public sealed class ChallengeModeBlocker : MonoBehaviour, ILoadable
{
    [Tooltip("챌린지 모드일 때 켤 가림막 오브젝트 (까만 Image, raycastTarget 켜져있어야 클릭도 막힘)")]
    [SerializeField] private GameObject blockerRoot;

    public int LoadOrder => 40;
    // OnEnable에서 캐싱해두고 그 뒤로는 이 캐시만씀 (씬종료 시 .instance 재호출로
    // Singleton<T>가 새 오브젝트를 만들어버리는 문제를 피하기 위함)
    //private StageManager _stageManager;
    private void Awake()
    {
        SceneLoadManager.Instance.RegisterLoadable(this);
    }

    private void OnEnable()
    {
        TryBindStageManager();
    }

    private void OnDisable()
    {
        UnbindStageManager();
    }


    public UniTask OnSceneLoadCreate(SceneId scene)=> UniTask.CompletedTask;
    public void Init(SceneId scene)
    {
        if (scene != SceneId.LobbySceneTest) return;
        TryBindStageManager();
        ApplyCurrentMode();
    }

    public void OnSceneDestory(SceneId scene)
    {
        UnbindStageManager();
    }

    private void TryBindStageManager()
    {
        if (ServiceLocator.TryGet<StageManager>(out StageManager stageMng))
        {
            stageMng.ModeChanged -= OnModeChanged;
            stageMng.ModeChanged += OnModeChanged;
            ApplyCurrentMode();
        }
    }

    private void UnbindStageManager()
    {
        if (ServiceLocator.TryGet<StageManager>(out StageManager stageMng))
        {
            stageMng.ModeChanged -= OnModeChanged;
        }
    }

    /// <summary>모드가 바뀌면(파밍 ↔ 챌린지) 가림막 상태를 다시 맞춘다</summary>
    /// <param name="mode">바뀐 후의 모드 (여기선 안 씀 - CurrentMode로 직접 확인)</param>
    private void OnModeChanged(StageMode mode)
    {
        ApplyCurrentMode();
    }

    /// <summary>현재 모드가 챌린지면 가림막을 켜고, 파밍이면 끈다</summary>
    private void ApplyCurrentMode()
    {
        if (blockerRoot == null) return;

        if (ServiceLocator.TryGet<StageManager>(out StageManager _stageManager))
        {
            blockerRoot.SetActive(_stageManager.CurrentMode == StageMode.Challenge);
        }
        else
        {
            blockerRoot.SetActive(false);
        }
    }
}
