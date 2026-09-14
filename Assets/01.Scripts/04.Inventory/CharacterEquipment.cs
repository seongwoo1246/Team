using System.Collections.Generic;
using UnityEngine;

public class CharacterEquipment : MonoBehaviour
{
    // 캐릭터별 장비 구분용
    private Dictionary<AttackType, Dictionary<EquipmentSlot, EquippedItem>> characterEquipments
        = new Dictionary<AttackType, Dictionary<EquipmentSlot, EquippedItem>>();

    // 캐릭터별 장착 장비
    [Header("장착 슬롯")]
    [SerializeField] private EquipmentInventorySlot weaponSlot;
    [SerializeField] private EquipmentInventorySlot armorSlot;
    [SerializeField] private EquipmentInventorySlot pantsSlot;
    [SerializeField] private EquipmentInventorySlot glovesSlot;
    [SerializeField] private EquipmentInventorySlot ringSlot;
    [SerializeField] private EquipmentInventorySlot shoesSlot;


    private void Awake()
    {
        // 각 캐릭터의 장비 저장 공간 생성
        CreateCharacterEquipment(AttackType.Physical);
        CreateCharacterEquipment(AttackType.Magic);
        CreateCharacterEquipment(AttackType.Heal);
    }


    private void CreateCharacterEquipment(AttackType attackType)
    {
        if (!characterEquipments.ContainsKey(attackType))
        {
            characterEquipments.Add(attackType, new Dictionary<EquipmentSlot, EquippedItem>());
        }
    }


    // 현재 캐릭터의 장착 장비 가져오기
    public EquippedItem GetEquippedItem(AttackType attackType, EquipmentSlot slot)
    {
        CreateCharacterEquipment(attackType);
        if (characterEquipments[attackType].ContainsKey(slot))
        {
            return characterEquipments[attackType][slot];
        }

        return null;
    }


    // 현재 캐릭터에게 장비 장착
    public void EquipItem(AttackType attackType, EquipmentSlot slot, EquippedItem item)
    {
        if (item == null)
            return;

        CreateCharacterEquipment(attackType);
        characterEquipments[attackType][slot] = item;
        RefreshEquipmentSlots(attackType);
    }


    // 현재 캐릭터의 장비 UI 갱신
    public void RefreshEquipmentSlots(AttackType attackType)
    {
        weaponSlot.SetEquippedItem(GetEquippedItem(attackType, EquipmentSlot.Weapon));
        armorSlot.SetEquippedItem(GetEquippedItem(attackType, EquipmentSlot.Armor));
        pantsSlot.SetEquippedItem(GetEquippedItem(attackType, EquipmentSlot.Pants));
        glovesSlot.SetEquippedItem(GetEquippedItem(attackType, EquipmentSlot.Gloves));
        ringSlot.SetEquippedItem(GetEquippedItem(attackType, EquipmentSlot.Ring));
        shoesSlot.SetEquippedItem(GetEquippedItem(attackType, EquipmentSlot.Shoes));
    }
}