using TMPro;
using UnityEngine;

public class GachaSlot : MonoBehaviour 
{
    // 결과창 보여주는 텍스트
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Sprite resulticon;

    [Header("희귀도에 따른 색상 변화")]
    [SerializeField] private Color NomalColor = Color.black;
    [SerializeField] private Color RareColor = Color.purple;
    [SerializeField] private Color LegendaryColor = Color.gold;
    
  



    public void Setup(GachaRewardItem rewardData)
    {
        //아이템 이름, 갯수, 표시
        Sprite resulticon = rewardData.itemIcon;
        string itemName = rewardData.itemName;
        resultText.text = $"{itemName}X{rewardData.amount} 획득";

        // 등급별 텍스트 색상 변화
        switch(rewardData.rarity)
        {
            case ItemRarity.Nomal: resultText.color = NomalColor; break;

            case ItemRarity.Rare: resultText.color = RareColor; break;

            case ItemRarity.Legendary: resultText.color = LegendaryColor; break;
        }
    }

   
}
