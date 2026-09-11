/* 담담자 - 송태훈
게임에 사용될 UserData 클래스
게임 실행 시(클라이언트) 서버에서 uid에 맞는 UserData를 받아와서 클라이언트에서 사용
매칭 후 GameLogicManager 또는 System에서 받아온 UserData를 통해 게임 진행
 */
using System;
[Serializable]
public class UserInfo
{
    public string UID { get; private set; }
    public UserProfileRequest Profile { get; private set; }
    public CharacterRequest Characters { get; private set; }
    public InventoryRequest Inventory { get; private set; }

    public UserInfo() { }

    public UserInfo(string uid, string nickname)
    {
        UID = uid;
        Profile = new UserProfileRequest(uid, nickname);
        Characters = new CharacterRequest(uid);
        Inventory = new InventoryRequest(uid);
    }
}
