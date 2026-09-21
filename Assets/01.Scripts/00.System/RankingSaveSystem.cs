using UnityEngine;
using System.Collections.Generic;
// 담당장 - 정성우
/*
랭킹을 로컬에서 Json파일로 저장하고 관리 하기 위해서 만든  스크립트

 */

public static class RankingSaveSystem
{
    private const string Save_Key = "LOCAL_RANKING_DATA_V1";

    /// <summary>
    /// 랭킹표를 json파일로 저장하는 함수
    /// </summary>
    /// <param name="damageList">데미지 리스트</param>
    /// <param name="clearTimeList">클리어 타임 리스트 </param>
    public static void SaveRankingData(List<UserRankData> damageList , List<UserRankData> clearTimeList)
    {
        LocalRankingDataWrapper wrapper = new LocalRankingDataWrapper
        {
            damageRankList = damageList ,
            clearRankList = clearTimeList
        };

        //객체를 json 문자열로 변환
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(Save_Key, json);
        PlayerPrefs.Save();

    }

    public static LocalRankingDataWrapper LoadRankingData()
    {
        if(!PlayerPrefs.HasKey(Save_Key))
        {
            return new LocalRankingDataWrapper(); // 저장 데이터가 없으면 새 객체가 반환
        }

        string json = PlayerPrefs.GetString(Save_Key);
        return JsonUtility.FromJson<LocalRankingDataWrapper>(json);
    }




}
    

