

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using Debug = DebugLogger<MailBoxManager>;


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

    // 파이어베이스 역직렬화를 위한 생성자
    public mailItem() { }
}


/// <summary>
/// 임시로 만든 스크립트(우편함 기능을 구현하기 위해 제작함)
/// </summary>
public class MailBoxManager : Singleton<MailBoxManager>
{
    private DatabaseReference dbRef;
    private string currentUserId = ""; // 나중에는 파이어베이스 Auth UID사용

    //로컬 우편캐시(mailId,mailItem)
    public Dictionary<string, mailItem> mailDictionary {  get; private set; } = new Dictionary<string, mailItem>();

    public static event Action OnMailboxUpdated;

    protected override void Awake()
    {
        base.Awake();
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
    }

    private void Start()
    {
        //실시간 우편 감지 시작
        StartListeningMails();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if( dbRef != null )
        {
            dbRef.Child("users").Child(currentUserId).Child("mails").ValueChanged -= OnMailDataChanged;
        }

    }

    /// <summary>
    /// 실시간 우편 데이터 변화 감지 구독
    /// </summary>
    private  void StartListeningMails()
    {
        dbRef.Child("users").Child(currentUserId).Child("mails").ValueChanged += OnMailDataChanged;
    }


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



    /// <summary>
    /// 단일 우편 수령시 코드
    /// </summary>
    /// <param name="mailId">우편 아이디</param>
    /// <returns></returns>
    public async Task<bool> ClaimMailAsync(string mailId)
    {
        if (!mailDictionary.TryGetValue(mailId, out mailItem mail)) return false;

        try
        {
            //경로 : user/{userId}/mails/{mailId}/isClaumed
            DatabaseReference targetMailRef = dbRef.Child("user").Child(currentUserId).Child("mails").Child(mailId).Child("isClaimed");

            // 서버 상태 업데이트 (true로 설정)
            await targetMailRef.SetValueAsync(true);
            //인게임 보상 지급
            GrantRewards(mail.rewards);

            mailDictionary.Remove(mailId);
            OnMailboxUpdated?.Invoke();

            return true;
        }
        catch(Exception ex)
        {
            Debug.LogWarning($"우편 수령중 문제 발생 : {ex.Message}");
            return false;
        }
    }


    /// <summary>
    /// 전체 메일 일괄 수령(Realtime DB 업데이트 맵 활용)
    /// </summary>
    public async void ClaimAllMails()
    {
        if (mailDictionary.Count == 0) return;

        //원자적 업데이트를 위한 경로 구성(구성별로 업데이트를 하기 위한 경로 설정)
        Dictionary<string,object> childUpdates = new Dictionary<string,object>();
        List<mailReward> allRewalds = new List<mailReward>();

        foreach (var kvp in mailDictionary)
        {
            mailItem mail = kvp.Value;
            //한번에 여러 경로를 업데이트
            string path = $"/users/{currentUserId}/mails/{mail.mailId}/isClaimed";
            childUpdates[path] = true;

            allRewalds.AddRange(mail.rewards);
        }

        try
        {
            //1번에 네크워크 통신으로 일괄 처리
            await dbRef.UpdateChildrenAsync(childUpdates);

            // 전체 보상 지급
             GrantRewards(allRewalds);

            mailDictionary.Clear();
            OnMailboxUpdated?.Invoke();
            Debug.Log("[MailBox] 모든 우편 수령 완료");
        }
        catch(Exception ex)
        {
            Debug.LogWarning($"전체 우편 수령 실패 : {ex.Message}");
        }
    }


    public void GrantRewards(List<mailReward> rewards)
    {
        foreach (mailReward reward in rewards)
        {
            switch(reward.rewardType)
            {
                case RewardType.Gold:
                    GameEvents.TriggerOnGoldObtained(reward.amount);
                    GoldWallet.instance.Add(reward.amount);
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
