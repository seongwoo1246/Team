using UnityEngine;

public class UI_Navigator : MonoBehaviour
{
    // 맨 아래 메뉴 버튼들 각각 패널 이동

    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject characterPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject rankingPanel;
    [SerializeField] private GameObject challengePanel;

    public void OpenMain()
    {
        CloseAllPanels();
        mainPanel.SetActive(true);
    }

    public void OpenCharacter()
    {
        if (characterPanel.activeSelf)
        {
            OpenMain();
            return;
        }

        CloseAllPanels();
        characterPanel.SetActive(true);
    }

    public void OpenShop()
    {
        if (shopPanel.activeSelf)
        {
            OpenMain();
            return;
        }

        CloseAllPanels();
        shopPanel.SetActive(true);
    }

    public void OpenRanking()
    {
        if (rankingPanel.activeSelf)
        {
            OpenMain();
            return;
        }

        CloseAllPanels();
        rankingPanel.SetActive(true);
    }

    public void OpenChallenge()
    {
        if (challengePanel.activeSelf)
        {
            OpenMain();
            return;
        }

        CloseAllPanels();
        challengePanel.SetActive(true);
    }

    private void CloseAllPanels()
    {
        mainPanel.SetActive(false);
        characterPanel.SetActive(false);
        shopPanel.SetActive(false);
        rankingPanel.SetActive(false);
        challengePanel.SetActive(false);
    }
}
