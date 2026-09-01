/*
전체 캐릭터 공용 "스킬 자동 사용" 온오프 토글. 캐릭터별로 따로 있는 게 아니라 하나만 존재
*/

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CharacterBase.AutoSkillEnabled(전체 캐릭터 공통 static 값)를 이 토글 UI와 연결한다
/// </summary>
public sealed class AutoSkillToggle : MonoBehaviour
{
    [Tooltip("연결된 Toggle UI")]
    [SerializeField] private Toggle toggle;

    private void Awake()
    {
        if (toggle == null)
        {
            return;
        }

        // 시작할 때 현재 오토 스킬 상태를 토글 겉모습에 반영
        toggle.isOn = CharacterBase.AutoSkillEnabled;
        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnDestroy()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleChanged);
        }
    }

    private void OnToggleChanged(bool isOn)
    {
        CharacterBase.AutoSkillEnabled = isOn;
    }
}
