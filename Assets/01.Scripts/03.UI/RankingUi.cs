using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/*
 사용 예시
데이터를 집어넣어야 하는 상황에서는 AddRecord(유저랭크데이터 리스트(데미지,플레이시간,클리어시간),값);을 실행하면
record 버튼 열 때 OnOpenRankUI();를 실행해서 랭킹표에 데이터를 보내서 탑 3만 남겨서 보여줄 예정
 */



/// <summary>
/// 여러 카테고리의 랭킹 데이터를 관리하고 탭 전환을 처리하는 UI 관리 스크립트
/// </summary>
public class RankingUi : MonoBehaviour
{
    /// <summary>
    /// 랭킹 슬롯 UI을 여기 넣어주면 된다.
    /// </summary>
    [Header("UI Solts(탑 3 고정 배열)")]
    [SerializeField] private RankingSlot[] slots = new RankingSlot[3];

    [Header("탭 버튼들")]
    [SerializeField] private Button damageTapBtn;
    [SerializeField] private Button playTimeTapBtn;
    [SerializeField] private Button clearTimeTapBtn;
    [Header("각 유저랭크데이터를 넣어줄 리스트")]
    public List<UserRankData> DamageList=new List<UserRankData>();
    public List<UserRankData> PlayTimeList=new List<UserRankData>();
    public List<UserRankData> ClearTimeList=new List<UserRankData>();

    //카테고리 종류별 탑 3를 가지는 딕셔너리
    private Dictionary<RankCategoty, UserRankData[]> top3CategoryDataDict = new Dictionary<RankCategoty, UserRankData[]>();
    
    private RankCategoty currentCategory = RankCategoty.Damage;

    private void Awake()
    {
        // 각 딕셔너리에 미리 크기 지정
        top3CategoryDataDict[RankCategoty.Damage] = new UserRankData[3];
        top3CategoryDataDict[RankCategoty.PlayTime] = new UserRankData[3];
        top3CategoryDataDict[RankCategoty.ClearTime] = new UserRankData[3];
        // 탭 버튼 이벤트 연결
        damageTapBtn.onClick.AddListener(()=> OnClickTap(RankCategoty.Damage));
        playTimeTapBtn.onClick.AddListener(()=> OnClickTap(RankCategoty.PlayTime));
        clearTimeTapBtn.onClick.AddListener(()=> OnClickTap(RankCategoty.ClearTime));
        
    }

    /// <summary>
    /// 각 데이터를 랭킹 리스트에 저장하는 역할, 랭킹을 켜줄 때 한번에 정렬 되어서 나올 예정
    /// </summary>
    /// <param name="dataList">데미지는 DamageList, 플레이 시간은 PlayTimeList, 클리어 시간은 ClearTimeList로 설정</param>
    /// <param name="score">값을 넣어주면 된다.</param>
    public void AddRecord(List<UserRankData> dataList ,double score)
    {
        dataList.Add(new UserRankData(score));
    }

    /// <summary>
    /// Record 버튼을 누를 때 지금까지 모은 데이터를 한 번에 보내줘서 초기화 하는 작업
    /// </summary>
    public void OnOpenRankUI()
    {
        SetCategoryScores(RankCategoty.Damage,DamageList,false);
        SetCategoryScores(RankCategoty.PlayTime,PlayTimeList,false);
        SetCategoryScores(RankCategoty.ClearTime, ClearTimeList, true);

    }

    /// <summary>
    /// 외부에서 수신한 데이터를 카테고리별 정렬 및 저장
    /// </summary>
    /// <param name="categoty">카테고리 종류</param>
    /// <param name="scores">userRankData들을 담은 리스트를 만들어서 넣어주면 됨</param>
    /// <param name="isAscending"> 기본내림차순, true로 하면 오름 차순</param>
    public void SetCategoryScores(RankCategoty categoty, List<UserRankData> scores, bool isAscending = false)
    {
        if (!top3CategoryDataDict.ContainsKey(categoty)) return;

        UserRankData[] buffer = top3CategoryDataDict[categoty];
        Array.Clear(buffer, 0, buffer.Length);// 기존 버퍼 정리

        // 탑 3 추려내기
        for(int i = 0; i < scores.Count; i++)
        {
            UserRankData currentData = scores[i];
            if (buffer[2].score != 0)
            {
                bool isWorstThan3rd = isAscending
                    ? (currentData.score >= buffer[2].score) // 오름차순(시간) : 3위보다 시간이 길거나 같으면 무시
                    : (currentData.score <= buffer[2].score);// 내림차순(점수) : 3위보다 점수가 낮거나 같으면 무시

                if (isWorstThan3rd) continue; // 연산 필요 없는 데이터 무시

            }

            // 3위 안에 확실하게 들어가는 데이터만 연산 실행
            for(int r =0; r<3;r++)
            {
                // 데이터가 없거나 조건에 맞는 경우 밀어내기 실행
                bool isHigher = isAscending 
                    ? (buffer[r].score == 0 || currentData.score < buffer[r].score)
                    : currentData.score > buffer[r].score;

                if (isHigher)
                {
                    // 한 단계식 밀어내기
                    for(int k  = 2; k > r; k--)
                    {
                        buffer[k] = buffer[k-1];
                    }
                    buffer[r] = currentData;
                    break;
                }

              
            }
            
        }
        // 현재 보고 있는 탭의 데이터를 업데이트 되었다면 화면 갱신
        if(categoty == currentCategory)
        {
            RefreshUI();
        }

    }

    /// <summary>
    /// 탭 하면 카테고리가 변하는 버튼 연동함수
    /// </summary>
    /// <param name="SelectedCategory"></param>
    public void OnClickTap(RankCategoty SelectedCategory)
    {
        if (currentCategory == SelectedCategory) return;

      
        currentCategory = SelectedCategory;
        RefreshUI();
    }

    /// <summary>
    /// 선택된 탭의 탑 3 데이터를 읽어 UI 슬롯 3개에 바인딩
    /// </summary>
    public void RefreshUI()
    {
        UserRankData[] currentTop3 = top3CategoryDataDict[currentCategory];

        // 컬러 매핑
        RankColor[] colors = new RankColor[] { RankColor.Gold, RankColor.Silver, RankColor.Bronze };

        for(int i = 0; i < 3; i++)
        {
            // 데이터가 있는 경우 슬롯 갱신 없으면 기본값
            if (currentTop3[i].score>0)
            {
                slots[i].SetUpSlot(i + 1, currentTop3[i].score, colors[i]);
            }
            else
            {
                slots[i].SetUpSlot(i+1,0,colors[i]);
            }
        }
    }
}
