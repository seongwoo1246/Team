/*
MainUI PartyHeaderPanel의 GoldText("Gold: 100G") 자리표시 텍스트를 실제 보유 골드로 바꿔주는 스크립트
GoldWallet.BalanceChanged 이벤트를 구독해서 골드가 바뀔 때마다(분당 골드 지급, 클리어 보너스 등) 자동 갱신됨
*/

using UnityEngine;
using TMPro;

/// <summary>
/// GoldWallet.Balance를 표시. GoldWallet.BalanceChanged를 구독해서 골드가 바뀔 때마다 갱신됨
/// </summary>
public sealed class GoldDisplay : MonoBehaviour
{
    [Tooltip("골드를 표시할 텍스트 (MainUI PartyHeaderPanel의 GoldText)")]
    [SerializeField] private TextMeshProUGUI goldText;

    // OnEnable에서 구독할 때 캐싱해두고 OnDisable에서 구독 해제할 때 이 캐시로만 접근한다.
    // GoldWallet.instance를 OnDisable에서 다시 호출하면, 씬이 꺼지는 순간 이미 원본이 파괴된 뒤라
    // Singleton<T>의 "없으면 새로 만드는" 로직이 발동해서 씬 종료 직전에 새 오브젝트가 하나 생겨버림
    private GoldWallet _goldWallet;

    private void OnEnable()
    {
        _goldWallet = GoldWallet.instance;
        if (_goldWallet != null)
        {
            _goldWallet.BalanceChanged += OnBalanceChanged;
            // 켜지는 순간(씬 로드 등)에도 현재 값 기준으로 한 번 맞춰줌
            OnBalanceChanged(_goldWallet.Balance);
        }
    }

    private void OnDisable()
    {
        if (_goldWallet != null)
        {
            _goldWallet.BalanceChanged -= OnBalanceChanged;
        }
    }

    /// <summary>골드가 바뀔 때마다 GoldWallet이 호출해줌. 천 단위 구분 기호를 넣어 표시</summary>
    /// <param name="balance">변경된 현재 골드</param>
    private void OnBalanceChanged(double balance)
    {
        if (goldText == null)
        {
            return;
        }

        goldText.text = $"Gold: {balance:N0}G";
    }
}
