using Cysharp.Threading.Tasks;
using UnityEngine;
using System;
 
/// <summary>
/// 게임의 전체 상태를 나타내는 상태판
/// </summary>
public enum GameState
{
    //에셋 다운로드 및 기본 설정 준비 중
    Init,
    // 유저 로그인 대기중
    Login,
    //파이어 베이스에서 유저 데이터 불러오는 중
    FatchUserData,
   //로비화면 (자동사냥)
    Lobby,
    //전투중(챌린지 모드( 싸움은 스테이지 매니저에서 해결)
    StageBattle
}





public class GameManager : Singleton<GameManager>
{

    [Header("연결할 다른 매니저들")]
    [SerializeField] private AddressableLoader addressableLoader;

    public GameState CurrentState {  get; private set; }

    private void Start()
    {
        StartGameSequenceAsync().Forget();
    }

    #region 1. 게임 전체 시퀸스 (순서대로 진행되는 메인 흐름)

    private async UniTaskVoid StartGameSequenceAsync()
    {
        //1단계 에셋 다운로드 및 초기 설정
        ChangeState(GameState.Init);
        DebugLogger<GameManager>.Log("[GameManager] : addressable 패치 및 다운로드 시작");
        bool isAssetReady = await addressableLoader.CheckAndDownLoadUpdateAsync(Progress =>
        {
            DebugLogger<GameManager>.Log($" 다운로드 진행율 : {Progress * 100}%");
        });

        if(!isAssetReady)
        {
            DebugLogger<GameManager>.Log(" 에셋 다운로드 실패했습니다. 인터넷을 확인하세요.");
            return;
        }

        //2단계 파이어베이스 로그인
        ChangeState(GameState.Login);
        DebugLogger<GameManager>.Log("[GameManager] : 로그인 화면으로 나왔습니다.");
        string userId = await WaitForUserLoginAsync();

        //3단계 유저 데이터 받아오기
        ChangeState(GameState.FatchUserData);
        DebugLogger<GameManager>.Log($"[GameManager] : {userId}님의 유저 데이터를 받아옵니다.");
        await FetchFirebaseUserDataAsync(userId);

        //4단계 로비 진입 및 자동 사냥 시작
        EnterLoddy();
    }
    #endregion

    #region 2. 파이어베이스 &데이터 처리단계(임의적으로 만든 함수로 나중에 다시 손 볼 예정)

    /// <summary>
    /// 유저가 아이디/비번을 치고 로그인 버튼을 누를 때 까지 대기하는 비동기함수
    /// </summary>
    /// <returns>로그인 한 아이디</returns>
    private async UniTask<string> WaitForUserLoginAsync()
    {
        //실제로는 UI와 연결해서 로그인 하는 상태
        await UniTask.Delay(TimeSpan.FromSeconds(1.5f));

        string LoggedInUserId = "대충 로그인 성공후 넘어온 아이디";
        return LoggedInUserId;
    }

    private async UniTask FetchFirebaseUserDataAsync(string userId)
    {
        //여기서 파이어베이스 서버와 아이디가 같은지 확인하고 정보 받는 부분
        await UniTask.Delay(TimeSpan.FromSeconds(1.5f));
        DebugLogger<GameManager>.Log("[GameManager] : 유저 데이터 로드 완료");
    }

    #endregion

    #region 3. 로비 및 스테이지 이동(전투 부분은 => 스테이지 매니저)

    /// <summary>
    /// 모든 작업 완료 후 로비로 이동하는 함수
    /// </summary>
    public void EnterLoddy()
    {
        ChangeState(GameState.Lobby);
        DebugLogger<GameManager>.Log("[GameManager] : 로비로 돌아왔습니다. (처치보상/분당보상)이 쌓이기 시작합니다.");
        //이 부분에서 게임 나가 있는 동안 쌓인 보상들 받는 함수
        ScenesManager.instance.LoadScenes(ScenesName.Lobby);

    }

    /// <summary>
    /// 스테이지 선택후 파밍에서 챌린지로 바뀌는 상태변화 함수
    /// </summary>
    /// <param name="stageId"></param>
    /// <returns></returns>
    public async UniTaskVoid StartSStageAsync(int  stageId)
    {
        ChangeState(GameState.StageBattle);
        DebugLogger<GameManager>.Log($"{stageId}번째 스테이지 입장");

       // 로비씬 초기화 하며 스테이지 시작
    }

    #endregion

    #region 4. 앱 상태 관리 (백그라운드 감지)

    /// <summary>
    /// 백그라운드로 화면이 전환 될때 할 함수
    /// </summary>
    /// <param name="focus"></param>
    private void OnApplicationFocus(bool focus)
    {
        if(!focus)
        {
            DebugLogger<GameManager>.Log("[GameManager] : 게임이 백그라운드로 돌아갔습니다. Ui와 진행상황 저장");
            // 혹은 타임스케줄을 0으로 만들어서 일시 정지 등등
        }
        else
        {
            DebugLogger<GameManager>.Log("[GameManager] : 유저가 게임으로 복귀했습니다.");
            //혹은 타임스케줄을 1으로 만들어서 일시 정지 해제 등등
        }
    }

    private void ChangeState(GameState state)
    {
        DebugLogger<GameManager>.Log("");
        CurrentState = state;
        DebugLogger<GameManager>.Log($"[상태 변경] => {state}");
    }

    #endregion

    //서버가 들고있어야 할 것
    /*
     처음에 받아올 것들(처음에 전체 데이터 리소스를 다운로드 최초1회)
    1. adressable에 있는 에셋번들(스프라이트, 애니메이션 클립, )
    2. 사운드
    다운로드 후 bool true로 바꾸기
    
    
    유저가 입력한 아이디를 키값으로 하는 딕셔너리를 만든후 로그인시 아이디가 같고 비밀번호가 틀리지 않다면 받아오는 정보들
    1. 유닛 데이터 (픽셀, 카툰)
    2. 스테이지 정보
    3. 재화정보
    4. 우편함 정보
    5. 

    */

    // 로컬이 들고 있어도 되는 것
    /*
     

     */



    //로고 나오는 동안 할 행동들
    /*

   DataManager -> GameManager 게임 시작 관련 데이터 정보 초기화 및 받아오기
  ex)사운드, 

   */


    //로그인 씬에서 해야 할 행동들
    /*
      
     GameManager -> Server 로그인 정보 보내기 , 정보 대조 , 맞을시 밑으로 연결 , 틀리면 재입력 요구
    <로딩중 화면> ScenesManager -> GameManager 로비씬 이동
    Server -> GameManager  유저정보 보내기, 스테이지 정보, 강화치 정보
    로비씬 Ui, 자동사냥 및 미접속 보상 정보 받기 ,  우편함 정보 받아오기

     */


    //로비씬 에서 해야 할 행동들
    /*
     

     자동 사냥 시스템 활성화 ( 분당 n원 만큼에 재화 획득)
    ScenesManager - > GameManager 스테이지 선택시 자기 씬 한 번 더 불러서 
    StageManager - > GameManager 초기화 작업 및 나오는 몬스터와 웨이브 정보 불러오기
    GameManager -> Server 스테이지 클리어 여부 서버로 전송
    Server -> GameManager 보스 클리어 후 배경과 몬스터 정보 받아오기



    백그라운드상태 일 때 
    UI 정지 혹은 비활성화 


     */








}
