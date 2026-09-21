using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Button claimButton;
    [SerializeField] private GameObject CompletedMark;

    



    // 재사용 시 데이터만 전달받아 UI 요소를 갱신합니다.
    public void  BindData(Achievement ach)
    {
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





}
