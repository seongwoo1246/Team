using UnityEngine;

public class EquipmentInventoryController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject inventoryPanel;

    [Header("Inventory")]
    [SerializeField] private GameObject inventoryGridPanel;

    public void OpenWeapon()
    {
        OpenInventory("����");
    }

    public void OpenArmor()
    {
        OpenInventory("����");
    }

    public void OpenPants()
    {
        OpenInventory("����");
    }

    public void OpenHelmet()
    {
        OpenInventory("����");
    }

    public void OpenGloves()
    {
        OpenInventory("�尩");
    }

    public void OpenBoots()
    {
        OpenInventory("�Ź�");
    }

    public void Open7()
    {
        OpenInventory("7");
    }

    private void OpenInventory(string equipmentType)
    {
        statsPanel.SetActive(false);
        inventoryPanel.SetActive(true);
    }

    public void CloseInventory()
    {
        inventoryPanel.SetActive(false);
        statsPanel.SetActive(true);
    }
}