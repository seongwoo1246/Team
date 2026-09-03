/*
방치형 게임 밸런스 모음 (순수 계산 함수, MonoBehaviour 아님)
출처: 우리 구글 시트 (_Config / Characters / Monsters )

돈(비용/골드)은 레벨이 오르면 지수로 커져서 int 범위를 금방 넘기므로 double을 쓴다
전투 수치(공격력/체력)는 float으로도 괜찮을거같음
*/

using UnityEngine;

/// <summary>
/// 레벨에 따른 강화 비용,능력치,체력,치명타 기대값을 계산하는 정적 클래스.
/// 상태를 갖지 않으므로 어디서든 안전하게 호출할 수 있고 단위 테스트가 쉽다.
/// </summary>
public static class StatCalculator
{
    //구글 시트 _Config 탭 기본값
    /// <summary>강화 비용 증가율 기본값 (_Config: cost_growth)</summary>
    public const float DEFAULT_COST_GROWTH = 1.15f;

    /// <summary>캐릭터 체력 증가율 기본값 (_Config: hp_growth)</summary>
    public const float DEFAULT_HP_GROWTH = 1.17f;

    /// <summary>몬스터 체력 증가율 기본값 (_Config: monster_hp_growth)</summary>
    public const float DEFAULT_MONSTER_HP_GROWTH = 1.20f;

    // ── 기본형: 공식을 그대로 옮긴 함수 ────────────────────────

    /// <summary>
    /// 강화 비용을 계산 기본비용 × (증가율 ^ 레벨)
    /// </summary>
    /// <param name="baseCost">레벨 0 기준 강화 비용 (시트: base_upgrade_cost)</param>
    /// <param name="growthRate">레벨당 비용 증가율 (시트: cost_growth, 예: 1.15)</param>
    /// <param name="level">현재 레벨 (0 이상)</param>
    /// return 이 레벨에서 다음 레벨로 강화하는 데 드는 골드
    public static double GetUpgradeCost(double baseCost, float growthRate, int level)
    {
        int safeLevel = Mathf.Max(0, level);
        return baseCost * System.Math.Pow(growthRate, safeLevel);
    }

    /// <summary>
    /// 레벨에 따른 능력치(물리·마법은 공격력, 힐러는 힐량)를 계산한다.
    /// 무기 공격력 + (상승치 × 레벨)
    /// </summary>
    /// <param name="baseValue">레벨 0 기준값 = 무기 공격력 (시트: base_power)</param>
    /// <param name="valuePerLevel">레벨당 상승치 (시트: power_per_level)</param>
    /// <param name="level">현재 레벨 (0 이상)</param>
    /// 치명타를 반영안한 순수 능력치
    public static float GetStatValue(float baseValue, float valuePerLevel, int level)
    {
        int safeLevel = Mathf.Max(0, level);
        return baseValue + (valuePerLevel * safeLevel);
    }

    /// <summary>
    /// 레벨에 따른 최대 체력을 계산한다. 기본체력 × (증가율 ^ 레벨)
    /// </summary>
    /// <param name="baseHp">레벨 0 기준 체력 (시트: base_hp)</param>
    /// <param name="hpGrowthRate">레벨당 체력 증가율 (시트: hp_growth, 예: 1.17)</param>
    /// <param name="level">현재 레벨 (0 이상)</param>
    /// 최대 체력
    public static float GetMaxHP(float baseHp, float hpGrowthRate, int level)
    {
        int safeLevel = Mathf.Max(0, level);
        return baseHp * Mathf.Pow(hpGrowthRate, safeLevel);
    }

    /// <summary>
    /// 치명타를 확률적으로 반영한 평균 데미지를 계산
    /// 기본공격력 × (1 + 치명타확률 × 치명타피해배수)
    /// 예) 100 × (1 + 0.1 × 1.0) = 110
    /// </summary>
    /// <param name="baseAtk">치명타를 반영하기 전 공격력</param>
    /// <param name="critRate">치명타 확률. 0.1 = 10% (시트: crit_chance)</param>
    /// <param name="critMulti">치명타 시 추가되는 피해 배수. 1.0 = 평타의 2배, 2.0 = 3배 (시트: crit_bonus)</param>
    /// 치명타 기대값이 섞인 실효 공격력
    public static float GetCritDamage(float baseAtk, float critRate, float critMulti)
    {
        // 치명타 확률은 100%를 넘을 수 없다
        float safeRate = Mathf.Clamp01(critRate);
        return baseAtk * (1f + (safeRate * critMulti));
    }

    /// <summary>
    /// 레벨에 따른 치명타 확률을 계산한다. 기본확률 + (레벨당증가 × 레벨), 최대 1.0(100%)에서 멈춤
    /// </summary>
    /// <param name="baseCritChance">레벨 0 기준 치명타 확률 (시트: crit_chance)</param>
    /// <param name="critChancePerLevel">레벨당 치명타 확률 증가치 (시트: crit_chance_per_level)</param>
    /// <param name="level">현재 레벨 (0 이상)</param>
    /// 0 ~ 1.0 사이로 잘린 치명타 확률
    public static float GetCritChance(float baseCritChance, float critChancePerLevel, int level)
    {
        int safeLevel = Mathf.Max(0, level);
        return Mathf.Clamp01(baseCritChance + (critChancePerLevel * safeLevel));
    }


    // ── 편의형: 캐릭터 SO + 파티 트랙 레벨 ──────────────────────
    // 파티 강화는 Power / Hp / Crit 트랙이 각각 따로 레벨업된다.
    // powerLevel = 공격력 트랙, hpLevel = 체력 트랙, critLevel = 치명타 트랙.

    /// <summary>
    /// 캐릭터 SO 기준으로 순수 능력치(공격력 또는 힐량)를 계산. powerLevel = Power 트랙 레벨.
    /// </summary>
    public static float GetStatValue(BaseStatData data, int powerLevel)
    {
        if (data == null)
        {
            return 0f;
        }
        return GetStatValue(data.BasePower, data.PowerPerLevel, powerLevel);
    }

    /// <summary>
    /// 캐릭터 SO 기준으로 최대 체력을 계산. hpLevel = Hp 트랙 레벨.
    /// </summary>
    public static float GetMaxHP(BaseStatData data, int hpLevel)
    {
        if (data == null)
        {
            return 0f;
        }
        return GetMaxHP(data.BaseHp, data.HpGrowthRate, hpLevel);
    }

    /// <summary>
    /// 캐릭터 SO 기준으로 치명타 확률을 계산 (100%에서 멈춤). critChanceLevel = Crit 트랙 레벨.
    /// </summary>
    public static float GetCritChance(BaseStatData data, int critChanceLevel)
    {
        if (data == null)
        {
            return 0f;
        }
        return GetCritChance(data.CritChance, data.CritChancePerLevel, critChanceLevel);
    }

    /// <summary>
    /// 캐릭터 SO 기준으로 치명타 피해 배수를 계산. critDamageLevel = CritDamage 트랙 레벨.
    /// 상한 없음 (1.0 = 크리 시 평타의 2배, 2.0 = 3배 ...).
    /// </summary>
    public static float GetCritBonus(BaseStatData data, int critDamageLevel)
    {
        if (data == null)
        {
            return 0f;
        }
        int safeLevel = Mathf.Max(0, critDamageLevel);
        return data.CritBonus + (data.CritBonusPerLevel * safeLevel);
    }

    /// <summary>
    /// 캐릭터 SO 기준으로 치명타 기대값이 반영된 실효 공격력(또는 힐량)을 계산.
    /// 공격력 = Power 트랙, 치명타 확률 = Crit 트랙, 치명타 피해 = CritDamage 트랙.
    /// </summary>
    public static float GetEffectiveStatValue(BaseStatData data, int powerLevel, int critChanceLevel, int critDamageLevel)
    {
        if (data == null)
        {
            return 0f;
        }
        float raw = GetStatValue(data, powerLevel);
        float critChance = GetCritChance(data, critChanceLevel);
        float critBonus = GetCritBonus(data, critDamageLevel);
        return GetCritDamage(raw, critChance, critBonus);
    }
}
