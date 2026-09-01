/*
캐릭터 스킬 버튼. 누르면 해당 캐릭터의 스킬을 시도하고 남은 쿨다운을 원형 게이지(Image Filled/Radial360)로 보여줌
*/

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 1명 x 스킬 1개(스킬1 또는 스킬2)에 대응하는 버튼. Button의 OnClick에 OnClickUseSkill()을 연결해서 쓴다
/// </summary>
public sealed class SkillButtonUI : MonoBehaviour
{
    private enum SkillSlot
    {
        Skill1,
        Skill2,
    }

    [Tooltip("이 버튼이 조작할 캐릭터")]
    [SerializeField] private CharacterBase target;

    [Tooltip("스킬1 버튼인지 스킬2 버튼인지")]
    [SerializeField] private SkillSlot skillSlot = SkillSlot.Skill1;

    [Tooltip("남은 쿨다운을 보여줄 원형 이미지 (Image Type: Filled, Fill Method: Radial 360)")]
    [SerializeField] private Image cooldownFillImage;

    [Tooltip("버튼 배경 이미지. 쿨다운은 다 찼는데 조건이 안 맞아 못 쓰는 상태(예: 부활 대상 없음)면 살짝 어둡게 표시하는 용도")]
    [SerializeField] private Image buttonImage;

    // buttonImage의 원래 색 (Awake 시점 값을 기준으로 삼아서, 못 쓰는 상태일 때만 알파를 낮췄다가 되돌림)
    private Color _buttonFullColor;

    private void Awake()
    {
        if (buttonImage != null)
        {
            _buttonFullColor = buttonImage.color;
        }
    }

    private void Update()
    {
        if (target == null)
        {
            return;
        }

        bool isSkill1 = skillSlot == SkillSlot.Skill1;
        float ratio = isSkill1 ? target.Skill1CooldownRatio : target.Skill2CooldownRatio;

        if (cooldownFillImage != null)
        {
            cooldownFillImage.fillAmount = ratio;
        }

        if (buttonImage != null)
        {
            bool usable = isSkill1 ? target.IsSkill1Usable : target.IsSkill2Usable;
            Color color = _buttonFullColor;
            if (!usable)
            {
                color.a = _buttonFullColor.a * 0.4f;
            }
            buttonImage.color = color;
        }
    }

    /// <summary>버튼 OnClick에 연결. 쿨다운이 다 찼으면 스킬을 사용한다</summary>
    public void OnClickUseSkill()
    {
        if (target == null)
        {
            return;
        }

        if (skillSlot == SkillSlot.Skill1)
        {
            target.TryUseSkill1();
        }
        else
        {
            target.TryUseSkill2();
        }
    }
}
