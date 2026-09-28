using TMPro;
using UnityEngine;
//담당자 - 정성우
/*
가챠를 돌리면 여기서 보여지면서 나올 예정이였던 스크립트 하지만 상점 매니저를 안쓰게 되면서 같이 뭍혀버린 스크립트이다. 

 */


public class GachaSlot : MonoBehaviour, IPoolable
{
    // 결과창 보여주는 텍스트
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Sprite resulticon;

    [Header("희귀도에 따른 색상 변화")]
    [SerializeField] private Color NomalColor = Color.black;
    [SerializeField] private Color RareColor = Color.purple;
    [SerializeField] private Color LegendaryColor = Color.gold;

    public void OnDespawn()
    {
        gameObject.SetActive(false);
    }

    public void OnSpawn()
    {
        gameObject.SetActive(true);
    }

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
