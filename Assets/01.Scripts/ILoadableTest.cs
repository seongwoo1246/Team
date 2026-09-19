/* 담당자 - 송태훈 / 설명 스크립트
 ServiceLocator 사용법 + ISyncable 사용법
ServiceLocator : 서비스를 등록할 경우 전역, 로컬 기준으로 싱글톤처럼 외부 참조를 할 수 있음. 단 초기화 순서를 '무조건' 맞춰야 함
ILoadable : ServiceLocator를 등록할 경우 SceneLoadManager에서 LoadSceneAsync 중 LoadOrder 순서에 맞게 
인터페이스 메서드(초기화 순서를 보장해주는 메서드)를 실행함

ISyncable 
(서버에 저장될) 데이터가 변경되는 일이 발생하는 컴포넌트일 경우 부착

 */

using Cysharp.Threading.Tasks;
using UnityEngine;

public class ILoadableTest : MonoBehaviour, ILoadable, ISyncable
{

    // 씬 로드할 때 초기화 순서를 정하는 Idx
    // 만약 다른 매니저나 Service Locator를 참조하게 될 경우 해당 컴포넌트보다 무조건 큰 숫자로 지정
    // 숫자가 낮을 수록 먼저 로드 됨(초기화 됨)
    // Manager나 System 같은 코어 시스템(Addressable이나 서버에서 직접 데이터를 받아오는) 30 이하의 숫자로
    // UI나 단순 로컬 데이터에서 받아오는 (UserManager.Instance.CurrentUser 에서 데이터를 받는) 클래스들은 50 이상의 숫자로
    public int LoadOrder => 100;
    private bool _isInitialized = false;    // ServiceLifetime이 Local일 경우 해당 flag 변수 사용
    private bool _isLoaded = false;         // ServiceLifetime이 Global일 경우 해당 flag 변수 사용
    private void Awake()
    {
        // Awake 단계에서 자기 자신을 등록한다.
        // Singleton일 경우에는 등록하지 않는다.
        // Service로 등록할 경우 [SerializeField]가 없어도 외부에서 참조할 수 있다.
        // 대신 참조하는 서비스보다 LoadOrder의 숫자가 커야한다.
        ServiceLocator.Register<ILoadableTest>(this, ServiceLifetime.Local);

        // 씬 매니저에 loadable을 등록한다.
        // ★★★★loadable은 싱글톤, 컴포넌트 상관없이 Lobby 씬에 배치되는 모든 컴포넌트(클래스)들을 등록해야함★★★★
        // loadable에 씬이 로드 될 때 LoadOreder에 따라 초기화 순서를 보장할 수 있다.
        SceneLoadManager.Instance.RegisterLoadable(this);

        // Awake에서는 SceneLoadManager에 ILoadable 등록과 ServiceLocator 만 등록하는 것 외에는
        // 아무 내용도 있으면 안 된다.
        // 대신 Awake에서 행해지던 모든 ILoadable의 일은 Init 단계에서 실행하면 된다.
    }
    private void Start()
    {
        // Start에서 행해지던 모든 일은 ILoadable의 Init 단계에서 실행하면 된다.
    }


    /// <summary>
    /// OnSceneLoadCreate에서 실제 행동하는 비동기 작업이 없을 경우 async를 제외하고 return  UniTask.CompletedTask;
    /// OnSceneLoadCreate에서 생성되거나 로드하는 내용이 있을 경우 async를 통해 비동기 작업을 수행
    /// </summary>
    public async UniTask OnSceneLoadCreate(SceneId scene)
    {
        // ServiceLifetime.Local 에서 특정 씬에서만 사용하는 클래스일 경우 SceneId로 flag 사용
        if (scene != SceneId.None) return;


        // 초기화 또는 로드 시 행해지는 내용을 단 1번만 진행하면 될 경우 flag 사용
        if (_isInitialized) return; 
        if (_isLoaded) return;

        /*
        씬 로드가 진행 중 행해져야 할 내용들을 넣어야 함. 
        보통 Addressable에서 데이터를 Load하거나 
        AddressableManger에서 Addressable을 로드해야할 경우 해당 내용을 이 메서드에서 진행
        또는 
         */

        //return UniTask.CompletedTask;
        await UniTask.CompletedTask;    // 실제로 async를 할 경우에는 UniTask.CompletedTask;를 넣는 것이 아닌 실제 비동기 작업을 넣어야 함
    }
    public void Init(SceneId scene)
    {
        /*
         Awake나 Start에서 실행하던 내용을 순서에 맞게 모두 여기다가 넣으면 된다.
        만약 OnEnable / OnDisable 에서 Service를 참조해서 이벤트를 등록하게 될 경우 하이라키에 생성된 Awake의 초기화 순서는 보장되지 않기 때문에 
        18일 마지막에 나던 StageManager를 찾을 수 없음 오류처럼 ServiceLocator에서 참조하는 해당 클래스를 찾을 수 없음 이라는 에러가 "무조건 1번"은 실행하게 됨
        초기화 순서가 보장되어 있으면 해당 메시지 에러는 1번 생성된 이후엔 생성되지 않음. 테스트 단위로 확인하고 싶으면
        ServiceLocator.TryGet<클래스> 에서 Debug를 찍기
        만일 이 메시지 조차도 보기 싫다하면 이벤트 등록을 OnEnable / OnDisable 에서 Service를 Get/TryGet 을 하지 않는다.
        팝업 UI On/Off를 예시로 들면 해당 팝업의 OnClickOpne / OnClickClode 와 같은 이벤트 메서드에서 실행하도록 한다.
         */

        // 초기화 완료 시 
        // 씬을 이동할 때 마다 초기화가 이루어져야 하면 넣지 말 것
        _isInitialized = true;
        _isLoaded = true;
    }

    // 
    public void OnSceneDestory(SceneId scene)
    {
        // ServiceLifetime이 Local일 경우 해당 씬이 파괴될 경우 등록한 서비스를 해제
        // 또는 데이터 해제나, 루프 해제, 이벤트 구독 헤제를 이 메서드에 넣는다.
        // -> Init 단계에서 이벤트를 구독해서 계속 유지한 후 파괴할 때 해제해야 하면(OnDestory 에서 진행했을 경우) 이 메서드에 넣기
        // 전역일 경우 해제하지 않아도 된다.
        // 현재는 씬이 2개 밖에 없어서 Local, Global의 의미는 크게 없지만 씬이 늘어나게 될 경우 유효하게 됨
        ServiceLocator.Unregister<ILoadableTest>();
    }

    public void SyncToUserMemory()
    {
        // 변경되는 데이터가 있을 경우 로컬 데이터에 먼저 저장 후 GameManager에서 전체 저장을 진행함
        // 현재는 예시로 CurrentUser를 접근하지만 상세한 값에 접근해야함
        UserInfo user = UserManager.Instance.CurrentUser;
        if (user != null)
        {
            // UserProfileRequest - nickname / currentStage / lastLoginTimestamp / gold / dia / playerLevel / currentExp / upgradeTrackLevels

            // <CharacterRequest>
            // CharacterRequest - characterDictionary(캐릭터 데이터 테이블) / BaseStatData(SO) 자체를 저장하지 않음
            // characterDictionary[characterId] - 캐릭터의 고유 ID (값 : string)
            // characterDictionary[isUnlock] - 캐릭터 해금 (값 : bool)
            //

            // <InventoryRequest>
            // InventoryRequest - InventorySaveData(데이터 테이블) / EquipmentData(SO) 자체를 저장하지 않음

            // [InventoryRequest] equipments 현재 보유 중인 장비의 딕셔너리 
            // 실제 값들을 저장하고 있는 DTO : EquipmentSaveDTO
            // 접근방법 : equipments[EquippedItem.instanceId(장비의 GUID] (값 : int)- 그 외에 내용은 DTO 클래스를 확인하거나 그래도 모르겠으면 물어봐주세용

            // [InventoryRequest] consumables 현재 보유 중인 소비 재료
            // 접근 방법 : inventory.Data.consumables["Material"]
            // [현재는 Material 밖에 존재하지 않아서 그냥 Material로 바로 접근 중] (값 : int)- 재료가 추가될 시 Consumable 이라는 소모 재료 추상 클래스를 만들어서 사용
        }
    }
}
