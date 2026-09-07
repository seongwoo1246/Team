using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum RankColor
{
    Gold,
    Silver,
    Bronze
}

/// <summary>
/// 단일 랭킹 슬롯을 담당 (1,2,3위를 표시할 예정)
/// </summary>
public class RankingSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI RankText;
    [SerializeField] private Image BackGround;
   
    public void SetUpSlot(int rank, double Score, RankColor color)
    {
        RankText.text = $"{rank}위 : {Score}";
        RankColorChange(color);
    }

    public void RankColorChange(RankColor color)
    {
        switch(color)
        {
            case RankColor.Gold:BackGround.color = Color.gold;  break;
            case RankColor.Silver:BackGround.color = Color.silver; break;
            case RankColor.Bronze:BackGround.color = Color.brown; break;
        }
    }




}
