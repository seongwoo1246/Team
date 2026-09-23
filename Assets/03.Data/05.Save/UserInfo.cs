/* 담담자 - 송태훈
게임에 사용될 UserData 클래스
게임 실행 시(클라이언트) 서버에서 uid에 맞는 UserData를 받아와서 클라이언트에서 사용
매칭 후 GameLogicManager 또는 System에서 받아온 UserData를 통해 게임 진행
 */
using Newtonsoft.Json;
using System;
[Serializable]
public class UserInfo
{
    public string UID { get; private set; }
    public UserProfileRequest Profile { get; private set; }
    public CharacterRequest Characters { get; private set; }
    public InventoryRequest Inventory { get; private set; }

    public UserInfo() 
    {
        Profile = new UserProfileRequest(string.Empty);
        Characters = new CharacterRequest(string.Empty);
        Inventory = new InventoryRequest(string.Empty);
    }

    public UserInfo(string uid, string nickname)
    {
        UID = uid;
        Profile = new UserProfileRequest(uid, nickname);
        Characters = new CharacterRequest(uid);
        Inventory = new InventoryRequest(uid);
    }
    [JsonConstructor]
    public UserInfo(string uid, UserProfileRequest profile, CharacterRequest characters, InventoryRequest inventory)
    {
        UID = uid;
        Profile = profile ?? new UserProfileRequest(uid);
        Characters = characters ?? new CharacterRequest(uid);
        Inventory = inventory ?? new InventoryRequest(uid);

        // 하위 객체들의 uid 동기화 보장
        if (Profile != null) Profile.uid = uid;
        if (Characters != null) Characters.uid = uid;
        if (Inventory != null) Inventory.uid = uid;
    }
}
