/*
구글 시트 _Config 탭의 전역 값을 담는 SO.
파티 강화 6트랙(Power/Hp/Crit/CritDamage/GoldGain/AttackSpeed)의 비용 파라미터를 다음

CSV 임포터가 _Config.csv (key,value 형식) 를 읽어 채움
  power_upgrade_base → powerUpgradeBase,  gold_gain_per_level → goldGainPerLevel
*/

using UnityEngine;

/// <summary>
/// 게임 전역 설정 데이터. 프로젝트 창에서 우클릭 → Create → Game/Game Config 로 생성.
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Game Config", order = 10)]
public class GameConfig : ScriptableObject
{
    [Header("공격력 강화")]
    [SerializeField] private double powerUpgradeBase = 30d;
    [SerializeField] private float powerUpgradeGrowth = 1.15f;

    [Header("체력 강화")]
    [SerializeField] private double hpUpgradeBase = 25d;
    [SerializeField] private float hpUpgradeGrowth = 1.15f;

    [Header("치명타 확률 강화")]
    [SerializeField] private double critUpgradeBase = 50d;
    [SerializeField] private float critUpgradeGrowth = 1.25f;

    [Header("치명타 피해 강화")]
    [SerializeField] private double critDamageUpgradeBase = 60d;
    [SerializeField] private float critDamageUpgradeGrowth = 1.20f;

    [Header("골드 획득량 강화")]
    [SerializeField] private double goldGainUpgradeBase = 40d;
    [SerializeField] private float goldGainUpgradeGrowth = 1.18f;

    [Tooltip("레벨당 골드 획득 배율 증가. 0.02 = 레벨당 +2%")]
    [SerializeField] private float goldGainPerLevel = 0.02f;

    [Header("공격 속도 강화")]
    [SerializeField] private double attackSpeedUpgradeBase = 45d;
    [SerializeField] private float attackSpeedUpgradeGrowth = 1.20f;

    [Tooltip("레벨당 공격 속도 계수 증가. 0.02 = 레벨당 +2% 빠르게 (간격을 1+0.02*Lv 로 나눔)")]
    [SerializeField] private float attackSpeedPerLevel = 0.02f;

    // 레벨당 골드 획득 배율 증가치
    public float GoldGainPerLevel => goldGainPerLevel;

    // >레벨당 공격 속도 계수 증가치
    public float AttackSpeedPerLevel => attackSpeedPerLevel;

    // 트랙별 0레벨 기준 강화 비용
    public double GetUpgradeBaseCost(UpgradeTrack track)
    {
        switch (track)
        {
            case UpgradeTrack.Power:
                return powerUpgradeBase;
            case UpgradeTrack.Hp:
                return hpUpgradeBase;
            case UpgradeTrack.Crit:
                return critUpgradeBase;
            case UpgradeTrack.CritDamage:
                return critDamageUpgradeBase;
            case UpgradeTrack.GoldGain:
                return goldGainUpgradeBase;
            case UpgradeTrack.AttackSpeed:
                return attackSpeedUpgradeBase;
            default:
                return 0d;
        }
    }

    // 트랙별 레벨당 비용 증가율
    public float GetUpgradeGrowth(UpgradeTrack track)
    {
        switch (track)
        {
            case UpgradeTrack.Power:
                return powerUpgradeGrowth;
            case UpgradeTrack.Hp:
                return hpUpgradeGrowth;
            case UpgradeTrack.Crit:
                return critUpgradeGrowth;
            case UpgradeTrack.CritDamage:
                return critDamageUpgradeGrowth;
            case UpgradeTrack.GoldGain:
                return goldGainUpgradeGrowth;
            case UpgradeTrack.AttackSpeed:
                return attackSpeedUpgradeGrowth;
            default:
                return 1f;
        }
    }
}
