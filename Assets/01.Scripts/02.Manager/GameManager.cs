using Cysharp.Threading.Tasks;
using UnityEngine;
 
public class GameManager : Singleton<GameManager>
{
    private bool isLoadDone = false;
    private bool FristDownload = false;






   





    /// <summary>
    ///  로딩화면 보여주고 씬 넘어갈 때 isLoadDone를 true로 바꿔주면 씬 넘어가는 비동기 유니테스크
    /// </summary>
    /// <param name="scenes">자신이 로딩후 이동할 씬 넣기</param>
    /// <returns></returns>
    private async UniTask LoadingDone(ScenesName scenes)
    {
        //로딩창 화면함수 
        await UniTask.WaitUntil(() => isLoadDone);
        //로딩창 화면함수 
        ScenesManager.instance.LoadScenes(scenes);
    }






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

    //챌린지 모드일 때 할 행동들
    /*
     



     
     
     
     
     
     */






}
