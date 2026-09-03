using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = DebugLogger<NicknamePopupUI>;

public class NicknamePopupUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private Button confirmButton;

    public event Action<string> OnNicknameConfirmed;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    private void OnDestroy()
    {
        confirmButton.onClick.RemoveListener(OnConfirmClicked);
    }


    public void Open()
    {
        nicknameInput.text = string.Empty;
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void OnConfirmClicked()
    {
        string nick = nicknameInput.text.Trim();
        if (string.IsNullOrEmpty(nick) || nick.Length < 2)
        {
            // 닉네임 유효성 검사 체크 처리
            return;
        }
        Debug.Log($"닉네임 입력 완료: {nick}, 클릭");

        OnNicknameConfirmed?.Invoke(nick);
    }
}
