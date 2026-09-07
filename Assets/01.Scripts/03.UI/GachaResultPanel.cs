using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 가챠 돌리고 나오는 패널에 붙여줄 스크립트
/// </summary>
public class GachaResultPanel : MonoBehaviour
{
  
      [Header("UI 레이아웃과 프리팹")]
     
    [SerializeField] private Button confirmButton; //확인 버튼

    private List<GachaSlot> spawnedTextItem = new List<GachaSlot>();

    private void Awake()
    {
        
        confirmButton.onClick.AddListener(CloseResultWindow);
    }   
    

    //가챠를 누르면 나와야하는 부분
    public void OpenResultWindow(List<GachaRewardItem> list)
    {
        
        if (list == null || list.Count == 0) return;

        gameObject.SetActive(true);
        ClearList();

        for(int i = 0; i < list.Count; i++)
        {
            GachaSlot item = ObjcetPoolManager.instance.Spawn<GachaSlot>(enumType.Item_Gear);
            item.OnSpawn();
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
                spawnedTextItem[i].OnDespawn();             
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
