using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementSlot : MonoBehaviour , IPoolable , ILoadable
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Button claimButton;
    [SerializeField] private GameObject CompletedMark;


    private int currentAchievementId;

    // 업적 매니저 보다 늦으면 상관없음
    public int LoadOrder => 31;

    private void OnprogressChanged(int updatedId)
    {
        if (AchievementManager.Instance.achievementsDictionary.TryGetValue(updatedId, out Achievement ach))
        {
            BindData(ach);
        }
    }

   

    // 재사용 시 데이터만 전달받아 UI 요소를 갱신합니다.
    public void  BindData(Achievement ach)
    {
        currentAchievementId = ach.id;

        titleText.text = ach.title;

        // 진행도 계산
        float progressRatio = Mathf.Clamp01((float)(ach.currentProgress / ach.targetProgress));
        progressBar.value = progressRatio;
        progressText.text = $"{ach.currentProgress}/{ach.targetProgress}";

        //보상 수령 상태에 따른 UI 분기
        if(ach.isClaimed)
        {
            claimButton.gameObject.SetActive(false);
            CompletedMark.SetActive(true);
        }
        else
        {
            CompletedMark.SetActive(false);
            claimButton.gameObject.SetActive(ach.isUnLocked);
            claimButton.interactable = ach.isUnLocked;
        }
    }

    public void OnSpawn()
    {
        AchievementManager.OnAchievementUpdated += OnprogressChanged;
    }

    public void OnDespawn()
    {
        AchievementManager.OnAchievementUpdated -= OnprogressChanged;
    }

    public UniTask OnSceneLoadCreate(SceneId scene)
    {
        throw new System.NotImplementedException();
    }

    public void Init(SceneId scene)
    {
        throw new System.NotImplementedException();
    }

    public void OnSceneDestory(SceneId scene)
    {
        throw new System.NotImplementedException();
    }
}
