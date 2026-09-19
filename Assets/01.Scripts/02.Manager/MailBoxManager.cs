

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Database;
using Debug = DebugLogger<MailBoxManager>;
using System.IO;
using System.Data;
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

       // LoadMailsFromLocal();
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
        //SaveMailsToLocal();

        // UI에 우편 왔다고 전달
        OnMailboxUpdated?.Invoke();

    }

    //public bool ClaimMailReward(string mailId)
    //{

    //}




    private void OnMailDataChanged(object sender , ValueChangedEventArgs args)
    {
        if(args.DatabaseError != null)
        {
            Debug.LogError($"데이터 로드 실패 = {args.DatabaseError.Message}");
            return;
        }

        mailDictionary.Clear();
        DataSnapshot snapshot = args.Snapshot;

        if(snapshot.Exists)
        {
            long currentUnixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach(DataSnapshot mailsnap in snapshot.Children)
            {
                //json파일을 객체값으로 전환
                string json = mailsnap.GetRawJsonValue();
                mailItem mail = JsonUtility.FromJson<mailItem>(json);
                mail.mailId = mailsnap.Key; // 키값을 mailId로 지정

                //만료 시간 검증
                if(mail.expireTimestamp>0&&mail.expireTimestamp < currentUnixTime)
                {
                    continue;
                }

                // 미수령 우편만 받음
                if(!mail.isClaimed)
                {
                    mailDictionary[mail.mailId] = mail;
                }
            }
        }

        Debug.Log($"우편함 동기화 왼료 / 안 받은 우편{mailDictionary.Count}개 있음");
        OnMailboxUpdated?.Invoke();

    }



  



    public void GrantRewards(List<mailReward> rewards)
    {
        foreach (mailReward reward in rewards)
        {
            switch(reward.rewardType)
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
    }

}
