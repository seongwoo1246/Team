// 작성자: 김주연
/*
챌린지 모드로 들어가면 정해둔 챌린지전용 배치로
바꾸고 파밍으로 돌아오면 원래(파밍) 배치로 되돌림
*/

using UnityEngine;

[System.Serializable]
public sealed class ChallengeLayoutTarget
{
    [Tooltip("위치를 바꿀 대상")]
    public Transform target;

    [Tooltip("챌린지 모드일 때 위치")]
    public Vector2 challengePosition;

    private Vector2 _farmingPosition;
    private RectTransform _rectTransform;

    public void CacheFarmingPosition()
    {
        if (target == null)
        {
            return;
        }

        _rectTransform = target.GetComponent<RectTransform>();
        _farmingPosition = _rectTransform != null ? _rectTransform.anchoredPosition : (Vector2)target.localPosition;
    }

    public void ApplyChallenge()
    {
        SetPosition(challengePosition);
    }

    public void ApplyFarming()
    {
        SetPosition(_farmingPosition);
    }

    private void SetPosition(Vector2 position)
    {
        if (target == null)
        {
            return;
        }

        if (_rectTransform != null)
        {
            _rectTransform.anchoredPosition = position;
        }
        else
        {
            target.localPosition = position;
        }
    }
}

public sealed class ChallengeLayoutSwitcher : MonoBehaviour
{
    [Header("챌린지 모드일 때 위치가 바뀌는 대상들")]
    [SerializeField] private ChallengeLayoutTarget[] layoutTargets;

    [Header("챌린지 모드에서만 꺼지는 오브젝트 (파밍 복귀 시 다시 켜짐)")]
    [SerializeField] private GameObject[] hideInChallenge;

    private StageManager _stageManager;

    private void Start()
    {
        for (int i = 0; i < layoutTargets.Length; i++)
        {
            layoutTargets[i].CacheFarmingPosition();
        }

        if (ServiceLocator.TryGet<StageManager>(out _stageManager))
        {
            _stageManager.ModeChanged += OnModeChanged;
            OnModeChanged(_stageManager.CurrentMode);
        }
        else
        {
            DebugLogger<ChallengeLayoutSwitcher>.LogError("StageManager를 ServiceLocator에서 찾을 수 없습니다.");
        }
    }

    private void OnDestroy()
    {
        if (_stageManager != null)
        {
            _stageManager.ModeChanged -= OnModeChanged;
        }
    }

    private void OnModeChanged(StageMode mode)
    {
        bool isChallenge = mode == StageMode.Challenge;

        for (int i = 0; i < layoutTargets.Length; i++)
        {
            if (isChallenge)
            {
                layoutTargets[i].ApplyChallenge();
            }
            else
            {
                layoutTargets[i].ApplyFarming();
            }
        }

        for (int i = 0; i < hideInChallenge.Length; i++)
        {
            if (hideInChallenge[i] != null)
            {
                hideInChallenge[i].SetActive(!isChallenge);
            }
        }
    }
}
