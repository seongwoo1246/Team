using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;




/// <summary>
/// 완성본이 아니고 제작 도중
/// </summary>
public class UserInventory : Singleton<UserInventory>
{
    [SerializeField] public TextMeshProUGUI money;
    [SerializeField] public TextMeshProUGUI coin;

   double gold = GoldWallet.instance.Balance;

    public Image icon;
    public TextMeshProUGUI Name;
    public TextMeshProUGUI des;

    public GameObject[] inventoryInspace;

    private int SelectItemId = -1;
    private int ItemID = -1;
   

    public void selectItme(int id)
    {
       
        SelectItemId = id;
        
        //var Data
        //if (Data != null)
        //{
        //    Name.text = Data.name;
        //    des.text = Data.description;
        //    icon.sprite = Data.icon;
        //}

    }

    public void UsedItem(int id)
    {
        // 소모품이면 줄어들고 장비템이면 장착하기 (enum으로 소모품인가 아닌가 검사)
    }

    //public void DiscountItemInventory(int id)
    //{
    //    for (int i = 0; i < inventoryInspace.Length; i++)
    //    {
    //        //������ Id�� ���ؿͼ� ������ ����ؼ� -1�ϰ� �� ���� �ʱ� ��(-1)�� ���� ����
    //        if (inventoryInspace[i].GetItemId() == id)
    //        {
    //            inventoryInspace[i].ItemChange(id, inventoryInspace[i].GetSprite(), -1);

    //            if (inventoryInspace[i].GetItemId() == -1)
    //            {
    //                SelectItemId = -1;
    //                // 자세하게 보여주던 설명창 나가기
    //            }
    //            break;
    //        }
    //    }
    //}

    //public void GetItem(int id, Sprite sprite, int count)
    //{
    //    //����ó��
    //    if (inventoryInspace == null || inventoryInspace.Length == 0) return;
    //    //�ߺ�üũ
    //    for (int i = 0; i < inventoryInspace.Length; i++)
    //    {
    //        if (inventoryInspace[i].GetItemId() == id)
    //        {
    //            inventoryInspace[i].ItemChange(id, sprite, count);
    //            return;
    //        }
    //    }
    //    //������ ����ֱ�
    //    for (int i = 0; i < inventoryInspace.Length; i++)
    //    {
    //        if (inventoryInspace[i].GetItemId() == -1 || inventoryInspace[i].GetSprite() == null)
    //        {
    //            inventoryInspace[i].ItemChange(id, sprite, count);

    //            return;
    //        }
    //    }


    //}

}
