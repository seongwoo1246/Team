using System;
using System.Collections.Generic;
//담당자 - 정성우

/*
유저의 랭킹 데이터를 정의 하는 현재로써는 데미지와 클리어 타임을 받아오도록 설계되어있다. 

 */


/// <summary>
/// 랭크 카테고리 종류(탭 확장 필요시 Enum추가만 하면 됨)
/// </summary>
public enum RankCategoty
{
    Damage,  // 최고 데미지 랭킹
    ClearTime// 클리어 타임 랭킹
}

/// <summary>
/// 카테고리별 유저 점수 데이터 구조체
/// </summary>
[Serializable]
public struct UserRankData
{
   
    public double score;

    public UserRankData( double score)
    {
        
        this.score = score;

    }
}

[Serializable]
public class LocalRankingDataWrapper
{
    public List<UserRankData> damageRankList = new List<UserRankData>();
    public List<UserRankData> clearRankList = new List<UserRankData>();
}
