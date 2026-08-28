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

    }

    public async UniTask AutoBattle()
    {
        while(isAutoBattling)
        {
            // 자동전투를 하며 분당 금액을 저장중
        }

        //스테이지 전투 혹은 
    }


}
