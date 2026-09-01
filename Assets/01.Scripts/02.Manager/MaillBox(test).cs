

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;


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
public class MailBoxTest : Singleton<MailBoxTest>
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
        //StartListeningMails();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

}
