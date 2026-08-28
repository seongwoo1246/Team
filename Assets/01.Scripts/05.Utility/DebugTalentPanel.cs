/*
[테스트 전용] 재능 강화 패널 흉내 (OnGUI, Canvas 없음).
화면 하단에 X1/X10/X100 배수 + Power/Hp/Crit 트랙별 강화 버튼을 그린다.

실제 UI 는 UI/UX 담당이 Canvas 로 만들고 테스트끝나면 삭제해요
*/

using UnityEngine;

/// <summary>
/// 파티 강화(3트랙)를 손으로 눌러 확인하는 임시 테스트 패널.
/// </summary>
public class DebugTalentPanel : MonoBehaviour
{
    [Header("디버그 UI")]
    [Tooltip("패널 확대 배율 (세로 모바일 해상도에서 작으면 키운다)")]
    [SerializeField] private float uiScale = 3f;

    [Tooltip("화면 하단에서 띄울 높이(px, 배율 적용 전)")]
    [SerializeField] private float bottomOffset = 8f;

    private static readonly int[] Multipliers = { 1, 10, 100 };
    private int _multiplier = 1;

    private CharacterBase _sample;

    private void OnEnable()
    {
        _sample = FindFirstObjectByType<CharacterBase>();
    }

    private void OnGUI()
    {
        if (GoldWallet.instance == null || UpgradeSystem.instance == null)
        {
            return;
        }

        Matrix4x4 prev = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(uiScale, uiScale), Vector2.zero);

        float w = 340f;
        float h = 300f;
        float x = 6f;
        float y = (Screen.height / uiScale) - h - bottomOffset;

        GUILayout.BeginArea(new Rect(x, y, w, h), GUI.skin.box);

        // 골드 + 치트
        GUILayout.BeginHorizontal();
        GUILayout.Label("골드 " + GoldWallet.instance.Balance.ToString("N0"));
        if (GUILayout.Button("+100000", GUILayout.Width(80f)))
        {
            GoldWallet.instance.Add(100000d);
        }
        GUILayout.EndHorizontal();

        // 배수 선택
        GUILayout.BeginHorizontal();
        for (int i = 0; i < Multipliers.Length; i++)
        {
            bool on = _multiplier == Multipliers[i];
            if (GUILayout.Toggle(on, " X" + Multipliers[i], GUI.skin.button))
            {
                _multiplier = Multipliers[i];
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4f);

        UpgradeSystem us = UpgradeSystem.instance;

        DrawTrack("강화 공격", UpgradeTrack.Power, _sample != null ? "공격 " + _sample.Power.ToString("F0") : "");
        DrawTrack("강화 HP", UpgradeTrack.Hp, _sample != null ? "HP " + _sample.MaxHP.ToString("F0") : "");
        DrawTrack("치명타율", UpgradeTrack.Crit, "확률 " + (SampleCritChance() * 100f).ToString("F0") + "%");
        DrawTrack("치명타 피해", UpgradeTrack.CritDamage, _sample != null ? "배수 " + SampleCritBonus().ToString("F2") : "");
        DrawTrack("골드 획득", UpgradeTrack.GoldGain, "×" + us.GetGoldMultiplier().ToString("F2"));
        DrawTrack("공격 속도", UpgradeTrack.AttackSpeed, "×" + us.GetAttackSpeedFactor().ToString("F2"));

        GUILayout.EndArea();

        GUI.matrix = prev;
    }

    private void DrawTrack(string title, UpgradeTrack track, string effect)
    {
        UpgradeSystem us = UpgradeSystem.instance;

        GUILayout.BeginHorizontal();
        GUILayout.Label(title + "  Lv." + us.GetLevel(track), GUILayout.Width(130f));
        GUILayout.Label(effect, GUILayout.Width(110f));

        double cost = us.GetCost(track);
        if (GUILayout.Button("강화 " + cost.ToString("N0")))
        {
            for (int n = 0; n < _multiplier; n++)
            {
                if (!us.TryUpgrade(track))
                {
                    break;
                }
            }
        }
        GUILayout.EndHorizontal();
    }

    private float SampleCritChance()
    {
        if (_sample == null || _sample.StatData == null || UpgradeSystem.instance == null)
        {
            return 0f;
        }
        return StatCalculator.GetCritChance(_sample.StatData, UpgradeSystem.instance.GetLevel(UpgradeTrack.Crit));
    }

    private float SampleCritBonus()
    {
        if (_sample == null || _sample.StatData == null || UpgradeSystem.instance == null)
        {
            return 0f;
        }
        return StatCalculator.GetCritBonus(_sample.StatData, UpgradeSystem.instance.GetLevel(UpgradeTrack.CritDamage));
    }
}
