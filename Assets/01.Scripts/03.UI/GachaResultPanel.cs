using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 가챠 돌리고 나오는 패널에 붙여줄 스크립트
/// </summary>
public class GachaResultPanel : MonoBehaviour
{
  
      [Header("UI 레이아웃과 프리팹")]
      [SerializeField] private Transform contentParent; // 스크롤뷰 콘텐트 위치
    [SerializeField] private GachaSlot TextPrefab; // 텍스트 프리팹
    [SerializeField] private Button confirmButton; //확인 버튼

    private List<GachaSlot> spawnedTextItem = new List<GachaSlot>();

    private void Awake()
    {
        confirmButton.onClick.AddListener(CloseResultWindow);
        
    }

    //임시로 만들었고 나중에 오브젝트 풀링으로 해서 다시 만들 예정
    public void OpenResultWindow(List<GachaRewardItem> list)
    {
        if (list == null || list.Count == 0) return;
        
        gameObject.SetActive(true);
        ClearList();

        for(int i = 0; i < list.Count; i++)
        {
            GachaSlot item = Instantiate(TextPrefab, contentParent);
            item.Setup(list[i]);
            spawnedTextItem.Add(item);
        }
    }

    private void ClearList()
    {
        for(int i = 0;i < spawnedTextItem.Count; i++)
        {
           if( spawnedTextItem[i] !=null )
            {
                Destroy(spawnedTextItem[i].gameObject);
            }
        }
        spawnedTextItem.Clear();
    }

    private void CloseResultWindow()
    {
        ClearList();
        gameObject.SetActive(false);
    }


}
