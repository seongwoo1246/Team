

using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = DebugLogger<MailBoxManager>;
using System.IO;

//담당자 - 정성우

public enum RewardType
{
    Gold,
    //유료 재화를 총칭
    Diamond,
    Item
}

[Serializable]
public class mailReward
{
    //보상 종류
    public RewardType rewardType; 
    // 아이템 코드 나머지는 0으로 통일
    public int itemCode;
    // 수량
    public int amount;
}

//우편 데이터 클래스 파이어베이스와 매칭해서 사용될 예정
[Serializable]
public class mailItem
{
    //고유 우편번호
    public string mailId;
    //우편 내용
    public string titile;
    //우편 내용물
    public string content;
    //보상 리스트
    public List<mailReward> rewards;
    //수령 여부
    public bool isClaimed;
    //만료기간( 초단위 기간)
    public long expireTimestamp;

   
}

/// <summary>
/// 우편 데이터 저장용 리스트
/// </summary>
[Serializable]
public class LoaclMailDataWrapper
{
    public List<mailItem> MailList = new List<mailItem>();
}


/// <summary>
/// 게임에서 우편 관련 총괄하여 사용할 매니저
/// </summary>
public class MailBoxManager : Singleton<MailBoxManager>
{
    //로컬 우편캐시(mailId,mailItem)
    public Dictionary<string, mailItem> mailDictionary { get; private set; } = new Dictionary<string, mailItem>();

    // 우편 상태가 바뀔 때 UI에 알려주는 신호
    public static event Action OnMailboxUpdated;

    private string saveFilePath;

    protected override void Awake()
    {
        base.Awake();

        // 안전한 위치에 저장경로 만들기
        saveFilePath = Path.Combine(Application.persistentDataPath, "local_mails.json");

        LoadMailsFromLocal();
    }

    public void AddMail(string title , string content, List<mailReward> rewards ,int validDays =7)
    {
        // 중복 되지 않는 우편 아이디를 만들어줌
        string newMailId = Guid.NewGuid().ToString();
        // 7일을 초로 바꿔서 만료기간 확인
        long expireTime = DateTimeOffset.UtcNow.AddDays(validDays).ToUnixTimeSeconds();

        // 편지 객체를 생성
        mailItem item = new mailItem
        {
            mailId = newMailId,
            titile = title ,
            content = content ,
            rewards = rewards ?? new List<mailReward>(),
            isClaimed = false,
            expireTimestamp = expireTime

        };

        // 딕셔너리에 새 편지 기록함
        mailDictionary[newMailId] = item;

        //변경된 편지 목록 즉시 저장함
        SaveMailsToLocal();

        // UI에 우편 왔다고 전달
        OnMailboxUpdated?.Invoke();

    }

    /// <summary>
    /// 보상이 한개 인 경우 사용하는 AddMail
    /// </summary>
    /// <param name="title">제목</param>
    /// <param name="content">내용</param>
    /// <param name="reward">보상종류</param>
    /// <param name="amount">수량</param>
    /// <param name="itemCode">아이템 Id (기본 =0)</param>
    /// <param name="validDays">만료기간 (기본 =7일)</param>
    public void AddMail(string title , string content , RewardType reward ,int amount,int itemCode = 0, int validDays = 7)
    {
        List<mailReward> singleRewardList = new List<mailReward>
        {
            new mailReward
            {
                rewardType = reward ,
                amount = amount ,
                itemCode = itemCode ,
            }
        };

        // 만들어서 보내주기
        AddMail(title,content, singleRewardList, validDays);
    }

    /// <summary>
    /// 보상이 없는 공지용 우편
    /// </summary>
    /// <param name="title"></param>
    /// <param name="content"></param>
    /// <param name="validDays"></param>
    public void AddMail(string title, string content , int validDays = 7)
    {
        AddMail(title, content ,null, validDays);
    }

    public bool ClaimMailReward(string mailId)
    {
        // 장부에서 해당 ID의 편지가 있는지 확인
        if (mailDictionary.TryGetValue(mailId, out mailItem mail))
        {
            // 이미 받은 거는 패스
            if (mail.isClaimed) return false;

            // 유통기한이 지난거는 패스
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if(currentTime> mail.expireTimestamp) return false;

            //보상 지급
            foreach(var reward in mail.rewards)
            {
                GiveRewardToPlayer(reward);
            }

            // 보상 받음 상태 전환
            mail.isClaimed = true;

            mailDictionary.Remove(mailId);

            // 로컬에 저장
            SaveMailsToLocal();
            //UI 새로고침
            OnMailboxUpdated?.Invoke();
            return true;
        }

        return false;
    }


    /// <summary>
    /// 보상 종류에 따라 플레이어에게 실제 재화 지급 함수
    /// </summary>
    /// <param name="reward"></param>
    private void GiveRewardToPlayer(mailReward reward)
    {
        switch (reward.rewardType)
        {
            case RewardType.Gold:
                GameEvents.TriggerOnGoldObtained(reward.amount);
                GoldWallet.Instance.Add(reward.amount);
                break;

            case RewardType.Diamond:
                // 유료 재화가 생길 시 여기서 추가하는 함수 넣기
                break;

            case RewardType.Item:
                // 인벤토리에 리워드 아이템 코드를 찾아서 받아오는 식
                break;
        }
    }

    /// <summary>
    /// 메일 정리 하는 함수
    /// </summary>
    public void CleanExpiredMails()
    {
        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() ;
        List<string> expiredIds = new List<string>() ;

        //장부를 뒤져서 만료 시간이 지난 편지 ID를 수집함
        foreach(var pair in mailDictionary)
        {
            if(currentTime>pair.Value.expireTimestamp)
            {
                expiredIds.Add(pair.Key);
            }
        }

        //지울 편지가 있다면 장부에서 완전히 삭제
        if(expiredIds.Count > 0)
        {
            foreach(string id in expiredIds)
            {
                mailDictionary.Remove(id);
            }

            SaveMailsToLocal();
            OnMailboxUpdated?.Invoke();
        }
    }

    /// <summary>
    /// 장부의 데이터를 텍스트 글자로 바꿔서 저장 파일로 만드는 함수
    /// </summary>
    private void SaveMailsToLocal()
    {
        //저장 가방 객체를 만들고 딕셔너리의 편지들을 리스트
        LoaclMailDataWrapper wrapper = new LoaclMailDataWrapper();
        wrapper.MailList.AddRange(mailDictionary.Values);

        // 객체를 json 형태의 문장으로 변환
        string json = JsonUtility.ToJson(wrapper,true);

        File.WriteAllText(saveFilePath, json);
    }

    /// <summary>
    /// 저장 파일의 글자 일어와 다시 장부에 채우는 함수
    /// </summary>
    private void LoadMailsFromLocal()
    {
        
        mailDictionary.Clear();

        // 저장된 파일이 없으면 패스
        if (!File.Exists(saveFilePath)) return;
        //파일의 텍스트 읽어오기
        string json = File.ReadAllText(saveFilePath);
        //json 문장을 다시 C# 객체로 만들어서 조립
        LoaclMailDataWrapper wrapper = JsonUtility.FromJson<LoaclMailDataWrapper>(json);

        if(wrapper != null&&wrapper.MailList!=null)
        {
            foreach(var mail in wrapper.MailList)
            {
                mailDictionary[mail.mailId] = mail;
            }
        }
        //불러오기 후 오래된 편지는 정리
        CleanExpiredMails();

    }
  

}
