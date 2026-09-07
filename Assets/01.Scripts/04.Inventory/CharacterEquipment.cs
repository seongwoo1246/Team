using UnityEngine;

public class CharacterEquipment : MonoBehaviour
{
    //캐릭터 착용 장비 저장용 스크립트

    [Header("현재 장착 장비")]
    [SerializeField] private EquippedItem weapon;
    [SerializeField] private EquippedItem armor;
    [SerializeField] private EquippedItem pants;
    [SerializeField] private EquippedItem gloves;
    [SerializeField] private EquippedItem ring;
    [SerializeField] private EquippedItem shoes;


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
                break;

            case EquipmentSlot.Armor:
                armor = item;
                break;

            case EquipmentSlot.Pants:
                pants = item;
                break;

            case EquipmentSlot.Gloves:
                gloves = item;
                break;

            case EquipmentSlot.Ring:
                ring = item;
                break;

            case EquipmentSlot.Shoes:
                shoes = item;
                break;
        }
    }
}