///*
//담담자 - 홍준호
// 캐릭터 패널 하단 스탯창 관리
// */

//using TMPro;
//using UnityEngine;

//public class CharacterStatsPanel : MonoBehaviour
//{
//    [Header("스탯 텍스트")]
//    [SerializeField] private TextMeshProUGUI hpText;
//    [SerializeField] private TextMeshProUGUI atkText;
//    [SerializeField] private TextMeshProUGUI attackSpeedText;
//    [SerializeField] private TextMeshProUGUI goldGainText;
//    [SerializeField] private TextMeshProUGUI critChanceText;
//    [SerializeField] private TextMeshProUGUI critDamageText;

//    [Header("캐릭터 선택")]
//    [SerializeField] private CharacterSelectController characterSelectController;

//    private void OnEnable()
//    {
//        RefreshStats();
//    }

//    public void RefreshStats()
//    {
//        if (characterSelectController == null)
//            return;

//        CharacterBase character =
//            characterSelectController.CurrentCharacter;

//        if (character == null)
//            return;

//        if (UpgradeSystem.Instance == null)
//            return;

//        // 현재 캐릭터의 실제 HP와 공격력
//        hpText.text = "HP: " + character.MaxHP.ToString("F0");
//        atkText.text = "ATK: " + character.Power.ToString("F1");

//        // 로비 공용 강화 레벨
//        int critLevel = UpgradeSystem.Instance.GetLevel(UpgradeTrack.Crit);
//        int critDamageLevel = UpgradeSystem.Instance.GetLevel(UpgradeTrack.CritDamage);

//        // 기준 캐릭터 데이터
//        BaseStatData characterStats = character.StatData;

//        if (characterStats == null)
//        {
//            // 연결 확인용, 확인 후 삭제
//            Debug.LogWarning("캐릭터의 StatData가 연결되지 않았습니다.");
//            return;
//        }

//        // 로비 강화로 계산되는 기본 치명타 수치
//        float baseCritChance = StatCalculator.GetCritChance(characterStats, critLevel) * 100f;
//        float baseCritDamage = StatCalculator.GetCritBonus(characterStats,critDamageLevel) * 100f;

//        // 장비 옵션 보너스
//        float attackSpeedEquipmentBonus = character.GetEquippedBonusRatio(EquipmentSlot.Shoes) * 100f;
//        float goldGainEquipmentBonus = character.GetEquippedBonusRatio(EquipmentSlot.Pants) * 100f;
//        float critChanceEquipmentBonus = character.GetEquippedBonusRatio(EquipmentSlot.Gloves) * 100f;
//        float critDamageEquipmentBonus = character.GetEquippedBonusRatio(EquipmentSlot.Ring) * 100f;

//        // 로비 공용 강화 배율
//        float attackSpeedUpgradeBonus = (UpgradeSystem.Instance.GetAttackSpeedFactor() - 1f) * 100f;
//        float goldGainUpgradeBonus = (float)((UpgradeSystem.Instance.GetGoldMultiplier() - 1d) * 100d);

//        // 최종 표시 수치
//        float finalAttackSpeed = attackSpeedUpgradeBonus + attackSpeedEquipmentBonus;
//        float finalGoldGain = goldGainUpgradeBonus + goldGainEquipmentBonus;
//        float finalCritChance = baseCritChance + critChanceEquipmentBonus;
//        float finalCritDamage = baseCritDamage + critDamageEquipmentBonus;

//        // UI 출력
//        attackSpeedText.text = "A.S: " + finalAttackSpeed.ToString("F1") + "%";
//        goldGainText.text = "G.G: " + finalGoldGain.ToString("F1") + "%";
//        critChanceText.text = "C.C: " + finalCritChance.ToString("F1") + "%";
//        critDamageText.text = "C.D: " + finalCritDamage.ToString("F1") + "%";
//    }
//}