/*
Party_Panel 스크롤 안에 있는 강화 카드 1개. UpgradeTrack(공격력/체력/치명타율/치명타피해/골드획득/공격속도)
6개 중 하나를 담당해서 이름 + 현재수치 -> 강화후수치 + 다음 강화비용을 보여주고, LevelUpButton으로
그 트랙만 강화한다 (트랙 레벨은 파티 공용이라 강화하면 파티원 전체에 동시 적용됨)

Power/Hp/Crit/CritDamage는 계산식이 캐릭터별 기본값(BaseStatData)을 필요로 해서 파티를 대표하는
값이 하나로 안 나옴 - 그래서 referenceCharacterStats(기준 캐릭터, 기본은 전사)로 예시 수치를 계산해
보여준다. 실제 강화 효과는 트랙 레벨 하나로 전 파티원에게 똑같이 적용되고, 여기 보이는 수치는 그 중
기준 캐릭터 기준 예시일 뿐이다.
GoldGain/AttackSpeed는 파티 공용 배율이라 referenceCharacterStats 없이도 정확한 값이 나온다
*/

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 강화 트랙 1개를 담당하는 카드. UpgradeSystem.TrackUpgraded를 구독해서 강화될 때마다(다른 카드가
/// 강화됐을 때도 - 플레이어 레벨 상한이 공용이라 표시가 바뀔 수 있음) 다시 그린다
/// </summary>
public sealed class UpgradeStatCard : MonoBehaviour
{
    [Tooltip("이 카드가 담당하는 강화 트랙")]
    [SerializeField] private UpgradeTrack track;

    [Tooltip("Power/Hp/Crit/CritDamage 예시 수치 계산용 기준 캐릭터 데이터 (GoldGain/AttackSpeed는 안 씀, 비워둬도 됨)")]
    [SerializeField] private BaseStatData referenceCharacterStats;

    [Tooltip("스탯 이름 + 현재 레벨을 표시할 텍스트")]
    [SerializeField] private TextMeshProUGUI statNameText;

    [Tooltip("현재 수치 -> 강화 후 수치를 표시할 텍스트")]
    [SerializeField] private TextMeshProUGUI statValueText;

    [Tooltip("다음 강화 비용을 표시할 텍스트")]
    [SerializeField] private TextMeshProUGUI upgradeCostText;

    [Tooltip("강화 버튼")]
    [SerializeField] private Button levelUpButton;

    // OnEnable에서 구독할 때 캐싱해두고 OnDisable에서 구독 해제할 때 이 캐시로만 접근한다.
    // UpgradeSystem.instance를 OnDisable에서 다시 호출하면, 씬이 꺼지는 순간 이미 원본이 파괴된 뒤라
    // Singleton<T>의 "없으면 새로 만드는" 로직이 발동해서 씬 종료 직전에 새 오브젝트가 하나 생겨버림
    private UpgradeSystem _upgradeSystem;

    private void OnEnable()
    {
        _upgradeSystem = UpgradeSystem.instance;
        if (_upgradeSystem != null)
        {
            _upgradeSystem.TrackUpgraded += OnTrackUpgraded;
        }

        if (levelUpButton != null)
        {
            levelUpButton.onClick.AddListener(OnClickLevelUp);
        }

        RefreshDisplay();
    }

    private void OnDisable()
    {
        if (_upgradeSystem != null)
        {
            _upgradeSystem.TrackUpgraded -= OnTrackUpgraded;
        }

        if (levelUpButton != null)
        {
            levelUpButton.onClick.RemoveListener(OnClickLevelUp);
        }
    }

    /// <summary>다른 카드가 강화돼도 다시 그림 (레벨 상한이 플레이어 레벨 공용이라 다른 카드 표시도 바뀔 수 있음)</summary>
    /// <param name="upgradedTrack">방금 강화된 트랙 (어떤 트랙이든 그냥 전부 다시 그림)</param>
    private void OnTrackUpgraded(UpgradeTrack upgradedTrack)
    {
        RefreshDisplay();
    }

    /// <summary>LevelUpButton의 OnClick에 연결. 이 카드가 담당하는 트랙만 강화 시도</summary>
    private void OnClickLevelUp()
    {
        if (UpgradeSystem.instance == null)
        {
            return;
        }

        UpgradeSystem.instance.TryUpgrade(track);
        RefreshDisplay(); // 성공/실패(골드 부족 등) 상관없이 최신 상태로 다시 그림
    }

    /// <summary>이름/현재수치→강화후수치/비용을 전부 다시 계산해서 표시</summary>
    private void RefreshDisplay()
    {
        if (UpgradeSystem.instance == null)
        {
            return;
        }

        int currentLevel = UpgradeSystem.instance.GetLevel(track);
        int nextLevel = currentLevel + 1;

        if (statNameText != null)
        {
            statNameText.text = $"{GetTrackDisplayName(track)} Lv.{currentLevel}";
        }

        if (statValueText != null)
        {
            statValueText.text = $"{GetValueTextAtLevel(currentLevel)} -> {GetValueTextAtLevel(nextLevel)}";
        }

        if (upgradeCostText != null)
        {
            upgradeCostText.text = $"다음 강화: {UpgradeSystem.instance.GetCost(track):N0}G";
        }
    }

    /// <summary>트랙 종류에 맞는 한글 표시 이름</summary>
    private string GetTrackDisplayName(UpgradeTrack targetTrack)
    {
        switch (targetTrack)
        {
            case UpgradeTrack.Power: return "공격력";
            case UpgradeTrack.Hp: return "체력";
            case UpgradeTrack.Crit: return "치명타 확률";
            case UpgradeTrack.CritDamage: return "치명타 피해";
            case UpgradeTrack.GoldGain: return "골드 획득";
            case UpgradeTrack.AttackSpeed: return "공격 속도";
            default: return targetTrack.ToString();
        }
    }

    /// <summary>지정한 레벨 기준 수치를 트랙 종류에 맞게 문자열로 계산 (Power/Hp는 원시값, 나머지는 %)</summary>
    /// <param name="level">계산 기준 레벨 (현재 레벨 또는 다음 레벨)</param>
    private string GetValueTextAtLevel(int level)
    {
        switch (track)
        {
            case UpgradeTrack.Power:
                return referenceCharacterStats == null ? "-" : StatCalculator.GetStatValue(referenceCharacterStats, level).ToString("F0");
            case UpgradeTrack.Hp:
                return referenceCharacterStats == null ? "-" : StatCalculator.GetMaxHP(referenceCharacterStats, level).ToString("F0");
            case UpgradeTrack.Crit:
                return referenceCharacterStats == null ? "-" : (StatCalculator.GetCritChance(referenceCharacterStats, level) * 100f).ToString("F0") + "%";
            case UpgradeTrack.CritDamage:
                return referenceCharacterStats == null ? "-" : (StatCalculator.GetCritBonus(referenceCharacterStats, level) * 100f).ToString("F0") + "%";
            case UpgradeTrack.GoldGain:
                return ((UpgradeSystem.instance.GetGoldMultiplierAtLevel(level) - 1d) * 100d).ToString("F0") + "%";
            case UpgradeTrack.AttackSpeed:
                return ((UpgradeSystem.instance.GetAttackSpeedFactorAtLevel(level) - 1f) * 100f).ToString("F0") + "%";
            default:
                return "-";
        }
    }
}
