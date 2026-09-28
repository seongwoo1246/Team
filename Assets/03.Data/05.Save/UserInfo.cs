/* 담담자 - 송태훈
클라이언트 런타임에서 유저의 전체 상태를 일원화하여 들고 있는 최상위 컨테이너 모델 클래스
프로필(Profile), 캐릭터 보유/장착 정보(Characters), 인벤토리(Inventory) 도메인을 하위 객체로 집약 관리
생성자 및 JsonConstructor를 통해 UID 바인딩의 무결성을 보장하며, 로컬/서버 간 직렬화·역직렬화의 기준 규격으로 사용
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
