/*
챌린지 스테이지 진입 시, 웨이브 진행을 슬라이더로 보여준다. 모든 스테이지가 3웨이브 고정이라
(StageRosterData에서 웨이브 수 증가를 꺼둠) 슬라이더도 3개 고정 정지 지점(1웨이브/2웨이브/3웨이브=보스)으로
단순하게 만들었다. 슬라이더의 핸들이 "내 캐릭터(점)" 역할을 해서, 웨이브를 깰 때마다 다음 지점으로 이동한다

주의: 만약 나중에 스테이지별로 웨이브 수가 다시 유동적으로 바뀌면 이 스크립트는 3웨이브 고정 전제가
깨지므로 다시 손봐야 한다 (StageManager.GetWaveCountForStage로 유동적으로 만드는 방식으로)
*/

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 웨이브 진행 상황을 슬라이더(3단계 고정: 1웨이브/2웨이브/3웨이브=보스)로 보여준다.
/// 챌린지 모드일 때만 보임
/// </summary>
public sealed class WaveSliderDisplay : MonoBehaviour
{
    [Header("슬라이더")]
    [Tooltip("웨이브 진행을 표시할 슬라이더 (플레이어가 못 만지게 Interactable은 꺼둘 것)")]
    [SerializeField] private Slider waveSlider;

    [Header("보스 정지 지점 강조")]
    [Tooltip("3번째(보스) 정지 지점에 표시할 큰 점 오브젝트의 Image (보스전 진입 시에만 빨갛게 바뀜)")]
    [SerializeField] private Image bossMarkerImage;
    [Tooltip("보스전에 들어가기 전(1/2웨이브) 보스 점 색 - 다른 점들과 같은 흐린 색")]
    [SerializeField] private Color bossUpcomingColor = new Color(1f, 1f, 1f, 0.6f);
    [Tooltip("보스전에 들어간 순간 보스 점 색 - 빨간색")]
    [SerializeField] private Color bossActiveColor = new Color(0.9f, 0.2f, 0.2f, 1f);

    // 마지막으로 슬라이더에 반영한 값. 안 바뀌면 다시 안 그려서 매 프레임 낭비를 피함
    private int _lastAppliedValue = int.MinValue;

    // OnEnable에서 캐싱해두고 그 뒤로는 이 캐시만 씀 (씬 종료 시 .instance 재호출로
    // Singleton<T>가 새 오브젝트를 만들어버리는 문제를 피하기 위함)
    private StageManager _stageManager;

    private void OnEnable()
    {
        _stageManager = StageManager.instance;
        if (_stageManager != null)
        {
            _stageManager.ChallengeStarted += OnChallengeStarted;
        }
    }

    private void OnDisable()
    {
        if (_stageManager != null)
        {
            _stageManager.ChallengeStarted -= OnChallengeStarted;
        }
    }

    /// <summary>챌린지가 새로 시작(재시작 포함)되면 슬라이더를 1웨이브 위치로 되돌린다</summary>
    /// <param name="stageNumber">시작한 스테이지 번호 (여기선 안 씀 - 모든 스테이지가 3웨이브 고정이라)</param>
    private void OnChallengeStarted(int stageNumber)
    {
        _lastAppliedValue = int.MinValue; // 다음 Update에서 무조건 다시 그리게 초기화
    }

    private void Update()
    {
        if (waveSlider == null || _stageManager == null)
        {
            return;
        }

        bool isChallengeMode = _stageManager.CurrentMode == StageMode.Challenge;

        // 매 프레임 무조건 실제 모드에 맞춰줌 (조건부로 하면 "우연히 초기값이 같은 경우" 처음에 안 맞춰지는 문제가 있음)
        waveSlider.gameObject.SetActive(isChallengeMode);

        if (!isChallengeMode)
        {
            return;
        }

        // CurrentWaveNumber: 1=1웨이브, 2=2웨이브, 0=보스전(3번째 지점) → 슬라이더 값(0~2)으로 변환
        int currentWaveNumber = _stageManager.CurrentWaveNumber;
        bool isBossPhase = currentWaveNumber == 0;
        int sliderValue = isBossPhase ? 2 : currentWaveNumber - 1;

        // 보스 점 색은 보스전에 들어갔을 때만 빨갛게. 슬라이더 값과 별개로 매 프레임 실제 상태에 맞춰준다
        // (조건부로 하면 "우연히 초기값이 같은 경우" 처음에 안 맞춰지는 문제가 있음)
        if (bossMarkerImage != null)
        {
            bossMarkerImage.color = isBossPhase ? bossActiveColor : bossUpcomingColor;
        }

        if (sliderValue == _lastAppliedValue)
        {
            return;
        }
        _lastAppliedValue = sliderValue;

        waveSlider.value = sliderValue;
    }
}
