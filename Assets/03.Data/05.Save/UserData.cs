/*
게임에 사용될 UserData 클래스
게임 실행 시(클라이언트) 서버에서 uid에 맞는 UserData를 받아와서 클라이언트에서 사용
매칭 후 GameLogicManager 또는 System에서 받아온 UserData를 통해 게임 진행
 */
using System;
using System.Collections.Generic;

[Serializable]
public class UserData
{
    public string uid;  // 고유 id. 이 값을 통해 UserData를 구분
    public string nickname;
    public int level;
    public int currentStage;
    public long lastLoginTimestamp; // 방치 보상 계산용

    // 재화 (골드, 다이아)
    public long gold;
    public long dia;

    // 인벤토리 관련 추후 추가 (무기, 장비, 성장 재료 등등)

    public UserData() { }

    public static UserData CreateNewUser(string uid, string nickname)
    {
        return new UserData
        {
            uid = uid,
            nickname = nickname,
            level = 1,
            currentStage = 1,
            lastLoginTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            gold = 0,
            dia = 0

            // 추가될 내용 있을 시 아래에 추가
        };
    }

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { nameof(uid), uid},
            { nameof(nickname), nickname },
            { nameof(level), level},
            { nameof(currentStage), currentStage},
            { nameof(gold), gold},
            { nameof(dia), dia},
        };
    }
}
