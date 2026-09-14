/*
캐릭터 스탯 총괄 화면(StatInfoPanel)에 공격력/체력/치명타율/치명타피해/공격속도/골드획득을 보여준다
전부 "기본 스탯 + 장비 보너스"가 합쳐진 최종값 - CharacterBase가 RecalculateStats()에서
이미 다 계산해둔값을 그대로 읽어오기만함 (여기서 새로 계산하는건 없음)
골드획득만 캐릭터 개인 스탯이 아니라 파티 전체에 적용되는 공통 값이라 StageManager에서 따로 읽어옴
*/

using UnityEngine;
using TMPro;

public sealed class CharacterStatInfoDisplay : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("지금 어느 캐릭터를 보고 있는지 알기 위한 참조")]
    [SerializeField] private CharacterSelectController characterSelectController;

    [Header("스탯 텍스트")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI atkText;
    [SerializeField] private TextMeshProUGUI critChanceText;
    [SerializeField] private TextMeshProUGUI critDamageText;
    [SerializeField] private TextMeshProUGUI attackSpeedText;
    [SerializeField] private TextMeshProUGUI goldGainText;

    private StageManager _stageManager;

    private CharacterBase _lastCharacter;
    private float _lastHp;
    private float _lastAtk;
    private float _lastCritChance;
    private float _lastCritBonus;
    private float _lastAttackSpeed;
    private double _lastGoldBonus = double.NegativeInfinity;

    private void OnEnable()
    {
        _stageManager = StageManager.instance;

        _lastCharacter = null;
        _lastGoldBonus = double.NegativeInfinity;
    }

    private void Update()
    {
        if (characterSelectController == null)
        {
            return;
        }

        CharacterBase character = characterSelectController.CurrentCharacter;
        if (character == null)
        {
            return;
        }

        double goldBonus = _stageManager != null ? _stageManager.PartyEquipmentGoldBonusRatio : 0d;

        bool changed = character != _lastCharacter
            || !Mathf.Approximately(character.MaxHP, _lastHp)
            || !Mathf.Approximately(character.Power, _lastAtk)
            || !Mathf.Approximately(character.CritChance, _lastCritChance)
            || !Mathf.Approximately(character.CritBonus, _lastCritBonus)
            || !Mathf.Approximately(character.AttackSpeedMultiplier, _lastAttackSpeed)
            || System.Math.Abs(goldBonus - _lastGoldBonus) > 0.0001d;

        if (!changed)
        {
            return;
        }

        _lastCharacter = character;
        _lastHp = character.MaxHP;
        _lastAtk = character.Power;
        _lastCritChance = character.CritChance;
        _lastCritBonus = character.CritBonus;
        _lastAttackSpeed = character.AttackSpeedMultiplier;
        _lastGoldBonus = goldBonus;

        if (hpText != null)
        {
            hpText.text = "체력: " + character.MaxHP.ToString("F0");
        }

        if (atkText != null)
        {
            atkText.text = "공격력: " + character.Power.ToString("F1");
        }

        if (critChanceText != null)
        {
            critChanceText.text = "치명타율: " + (character.CritChance * 100f).ToString("F1") + "%";
        }

        if (critDamageText != null)
        {
            critDamageText.text = "치명타피해: " + (character.CritBonus * 100f).ToString("F0") + "%";
        }

        if (attackSpeedText != null)
        {
            attackSpeedText.text = "공격속도: " + character.AttackSpeedMultiplier.ToString("F2") + "배";
        }

        if (goldGainText != null)
        {
            goldGainText.text = "골드획득: +" + (goldBonus * 100d).ToString("F1") + "% (공통)";
        }
    }
}
