using UnityEngine;

public class CharacterEquipment : MonoBehaviour
{
    // 캐릭터 착용 장비 저장용 스크립트

    [Header("현재 장착 장비")]
    [SerializeField] private EquippedItem weapon;
    [SerializeField] private EquippedItem armor;
    [SerializeField] private EquippedItem pants;
    [SerializeField] private EquippedItem gloves;
    [SerializeField] private EquippedItem ring;
    [SerializeField] private EquippedItem shoes;

    [Header("장착 슬롯")]
    [SerializeField] private EquipmentInventorySlot weaponSlot;
    [SerializeField] private EquipmentInventorySlot armorSlot;
    [SerializeField] private EquipmentInventorySlot pantsSlot;
    [SerializeField] private EquipmentInventorySlot glovesSlot;
    [SerializeField] private EquipmentInventorySlot ringSlot;
    [SerializeField] private EquipmentInventorySlot shoesSlot;


    private void Start()
    {
        RefreshEquipmentSlots();
    }


    public EquippedItem GetEquippedItem(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon:
                return weapon;

            case EquipmentSlot.Armor:
                return armor;

            case EquipmentSlot.Pants:
                return pants;

            case EquipmentSlot.Gloves:
                return gloves;

            case EquipmentSlot.Ring:
                return ring;

            case EquipmentSlot.Shoes:
                return shoes;
        }

        return null;
    }


    public void EquipItem(EquipmentSlot slot, EquippedItem item)
    {
        if (item == null)
            return;

        switch (slot)
        {
            case EquipmentSlot.Weapon:
                weapon = item;
                weaponSlot.SetEquippedItem(item);
                break;

            case EquipmentSlot.Armor:
                armor = item;
                armorSlot.SetEquippedItem(item);
                break;

            case EquipmentSlot.Pants:
                pants = item;
                pantsSlot.SetEquippedItem(item);
                break;

            case EquipmentSlot.Gloves:
                gloves = item;
                glovesSlot.SetEquippedItem(item);
                break;

            case EquipmentSlot.Ring:
                ring = item;
                ringSlot.SetEquippedItem(item);
                break;

            case EquipmentSlot.Shoes:
                shoes = item;
                shoesSlot.SetEquippedItem(item);
                break;
        }
    }


    private void RefreshEquipmentSlots()
    {
        weaponSlot.SetEquippedItem(weapon);
        armorSlot.SetEquippedItem(armor);
        pantsSlot.SetEquippedItem(pants);
        glovesSlot.SetEquippedItem(gloves);
        ringSlot.SetEquippedItem(ring);
        shoesSlot.SetEquippedItem(shoes);
    }
}