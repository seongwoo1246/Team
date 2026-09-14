using System.Collections.Generic;
using UnityEngine;
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

    [Header("라벨 단위 일괄 로드할 데이터 (Addressable Labels)")]
    [Tooltip("예: StatData, EquipmentData 등 해당 라벨에 속한 SO들을 일괄 로드")]
    [SerializeField] private List<string> dataLabels = new();

    [Header("개별 키로 로드할 데이터 (선택 사항)")]
    [Tooltip("라벨이 아닌 특정 어드레스 키로 로드해야 하는 단일 SO/TextAsset이 있을 경우")]
    [SerializeField] private List<string> individualAssetKeys = new();

    public SceneId TargetScene => targetScene;
    public IReadOnlyList<string> DataLabels => dataLabels;
    public IReadOnlyList<string> IndividualAssetKeys => individualAssetKeys;
}
