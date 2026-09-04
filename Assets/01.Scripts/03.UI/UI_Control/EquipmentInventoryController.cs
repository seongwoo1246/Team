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
        OpenInventory("무기");
    }

    public void OpenArmor()
    {
        OpenInventory("상의");
    }

    public void OpenPants()
    {
        OpenInventory("하의");
    }

    public void OpenHelmet()
    {
        OpenInventory("투구");
    }

    public void OpenGloves()
    {
        OpenInventory("장갑");
    }

    public void OpenBoots()
    {
        OpenInventory("신발");
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