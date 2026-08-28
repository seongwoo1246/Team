using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;




/// <summary>
/// 스테이지의 흐름  웨이브 관리등을 할 클래스
/// </summary>
public class StageManagerSeongWoo : Singleton<StageManagerSeongWoo>
{
  
   

    private bool isAutoBattling = true;
    private bool isStageClear = false;
    private bool isWaveClear = false;


    public void SelectStage()
    {
        // 스테이지 선택
        // 성에서 바닥으로 나가서 스테이지로 이동하는 애니메이션 연출
        // 스테이지 선택 후 이동시 초기화
        ScenesManager.instance.LoadScenes(ScenesName.Lobby);
    }

    public async UniTask AutoBattle()
    {
        while(isAutoBattling)
        {
            // 자동전투를 애니메이션 연출
        }

        //스테이지 전투 혹은 백그라운드시 멈춰있을 예정
    }


}
