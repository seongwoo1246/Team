using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UtilDebug = DebugLogger<NicknamePopupUI>;

public class NicknamePopupUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text duplicaiotnCheck;

    // LoginController에서 참조
    public event Action<string> OnNicknameConfirmed;

    private void OnEnable()
    {
        confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    private void OnDisable()
    {
        confirmButton.onClick.RemoveListener(OnConfirmClicked);
    }


    public void Open()
    {
        nicknameInput.text = string.Empty;
        duplicaiotnCheck.text = string.Empty;
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
            return;
        }
        UtilDebug.Log($"닉네임 입력 완료: {nick}, 클릭");

        OnNicknameConfirmed?.Invoke(nick);
    }

    public void ShowDuplication(string duplication)
    {
        duplicaiotnCheck.text = duplication;
        UtilDebug.Log($"닉네임 중복 {duplication}");
    }
}
