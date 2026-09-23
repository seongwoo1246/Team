using TMPro;
using UnityEngine;
using UnityEngine.UI;
// 담당자 -정성우

public class AchievementSlot : MonoBehaviour , IPoolable
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private GameObject CompletedMark;


    private int currentAchievementId;

    private void OnprogressChanged(int updatedId)
    {
        if (currentAchievementId != updatedId) return;
        if(ServiceLocator.TryGet<AchievementManager>(out AchievementManager achievementManager))
        {
            if(achievementManager.achievementsDictionary.TryGetValue(updatedId, out Achievement ach))
            {
                BindData(ach);
            }
        }
        else
        {
            Debug.Log("아카이브 매니저 로케이터 에러 발생");
        }
    }

   

    // 재사용 시 데이터만 전달받아 UI 요소를 갱신합니다.
    public void  BindData(Achievement ach)
    {
        currentAchievementId = ach.id;

        Debug.Log($"{currentAchievementId}");

        titleText.text = ach.title;

        // 진행도 계산
        float progressRatio = Mathf.Clamp01((float)(ach.currentProgress / ach.targetProgress));
        progressBar.value = progressRatio;
        progressText.text = $"{ach.currentProgress}/{ach.targetProgress}";

        //보상 수령 상태에 따른 UI 분기
        if(ach.isClaimed)
        {
            
            CompletedMark.SetActive(true);
        }
        else
        {
            CompletedMark.SetActive(false);
          
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
}
