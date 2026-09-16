using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PoolInfo
{
    public enumType poolType;
    public string addressableKey;
    public int initialCount;
    public bool isGlobal;
}

/// <summary>
/// 각 씬 진입 시 로드해야 할 데이터 에셋(SO, CSV 등)의 Addressable 라벨 및 키를 정의하는 SO
/// 프로젝트 창 우클릭 -> Create -> Game/Scene Data Config 로 생성
/// (예: LobbyScene_Config, BattleScene_Config 등)
/// </summary>
[CreateAssetMenu(fileName = "NewSceneDataConfig", menuName = "Game/Scene Data Config", order = 1)]
public class SceneDataConfigSO : ScriptableObject
{
    [Header("씬 식별자")]
    [SerializeField] private SceneId targetScene;

    [Header("Addressable 라벨 목록 ( 해당 라벨의 SO를 일괄 로드 )")]
    [Tooltip("예: StatData, MonsterData, EquipmentData ")]
    [SerializeField] private List<string> dataLabels = new();

    [Header("개별 Addressable 키 목록 ( 단일 Config SO 등)")]
    [Tooltip("예: GameConfig, StageRosterData ")]
    [SerializeField] private List<string> individualAssetKeys = new();

    [SerializeField] private List<PoolInfo> scenePoolList = new();
    public IReadOnlyList<PoolInfo> ScenePoolList => scenePoolList;

    public SceneId TargetScene => targetScene;
    public IReadOnlyList<string> DataLabels => dataLabels;
    public IReadOnlyList<string> IndividualAssetKeys => individualAssetKeys;
}
