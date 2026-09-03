/*
드랍되거나 장착된 장비 개별 인스턴스. MonoBehaviour도 so도 아닌 순수 C# 클래스
EquipmentData(SO)는 여러 개체가 공유하는 고정 정보고, 실제로 몇% 옵션으로 떴는지는 개체마다 달라서
그 값(rollPercent)만 따로 들고 다니게 햇음

인벤토리 시스템에서 세이브/전달할 땐 이 클래스를 통째로 쓰지 말고
Data.Id(문자열) + RollPercent만 저장했다가, 불러올 때 id로 EquipmentData를 다시찾아 재구성할것
*/

using System;
using UnityEngine;

/// <summary>
/// 장비 1개의 실제 인스턴스 (어떤 EquipmentData인지 + 몇 %로 떴는지)
/// </summary>
[Serializable]
public sealed class EquippedItem
{
    [Tooltip("이 인스턴스가 어떤 장비인지 (고정 정보)")]
    [SerializeField] private EquipmentData data;

    [Tooltip("드랍될 때 굴린 랜덤 보너스 (1~10 사이, %). Data.Slot이 담당하는 스탯에 이 값만큼 % 로 적용됨")]
    [SerializeField] private float rollPercent;

    /// <summary>
    /// 장비 인스턴스를 만든다. 보통 몬스터 드랍 시 랜덤 롤로 생성함
    /// </summary>
    /// <param name="data">어떤 장비인지 (고정 정보)</param>
    /// <param name="rollPercent">이번에 뜬 랜덤 보너스 (1~10 사이, %)</param>
    public EquippedItem(EquipmentData data, float rollPercent)
    {
        this.data = data;
        this.rollPercent = rollPercent;
    }

    // 어떤 장비인지 (고정 정보)
    public EquipmentData Data => data;

    // 굴린 랜덤 보너스 (1~10 사이, %)
    public float RollPercent => rollPercent;
}
