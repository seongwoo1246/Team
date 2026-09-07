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
   
    // 랭킹을 나타낸 텍스트
    [SerializeField] private TextMeshProUGUI RankText;
    // 뒤에 색상이 바뀔 이미지
    [SerializeField] private Image BackGround;

    //랭킹표 1,2,3위에 따라서 이미지 색상 변경
    double[] DamageRank = new double[2];

    double[] PlayTimeRank = new double[2];

    int[] ClearTimeRank = new int[2];

    public void SetupSlot(int rank,double score, RankColor color)
    {

    }

    public void RankColorChange(RankColor color)
    {
        switch(color)
        {
            case RankColor.Gold: BackGround.color = Color.gold; break;

            case RankColor.Silver: BackGround.color = Color.silver; break;

            case RankColor.Bronze: BackGround.color = Color.brown; break;


        }
    }








}
