
using TMPro;
using UnityEngine;
using UnityEngine.UI;




/// <summary>
/// 유저의 인터페이스와 관련된 클래스(아이템 관련되서 완성 되면 다시 고쳐야 하는 클래스)
/// </summary>
public class UserInventory : Singleton<UserInventory>
{
    [Header("재화 종류들")]
    [SerializeField] public TextMeshProUGUI money;
    [SerializeField] public TextMeshProUGUI coin;
    
    // 나중에 text와 실수 값을 연동해줘야 함
   double gold = GoldWallet.instance.Balance;

    // 인벤토리에서 보여질 화면
    public Image icon;
    public TextMeshProUGUI Name;
    //아이템에 대한 설명
    public TextMeshProUGUI des;

    /// <summary>
    /// 인벤토리 슬롯을 담아두는 배열로 인벤토리를 칸 순서 대로 넣어주지 않으면 꼬일 수 있기 때문에 인벤토리 순서대로 넣어줘야 함
    /// </summary>
    public InventorySlot[] inventoryInspace;

    private int SelectItemId = -1;
    
   
    /// <summary>
    /// 아이템을 선택했을 때 사용하는 함수
    /// </summary>
    /// <param name="id"></param>
    public void selectItme(int id)
    {
       
        SelectItemId = id;
        
        //var Data = 아이템 데이터에서 아이템을 들고 와야함
        //if (Data != null)
        //{
        //    Name.text = Data.name;
        //    des.text = Data.description;
        //    icon.sprite = Data.icon;
        //}

    }

    /// <summary>
    /// 소모품을 사용할 때 하는 함수
    /// </summary>
    public void UsedItem()
    {
        if (SelectItemId == -1)
            return;


        /*
         var data = 아이템 정보 불러오기(SelectItemId)
         switch(data.id)
        {
       case data.id : 아이템 효과 발동 break;
        } 
        DiscountItemInventory(SelectItemId)
         */
    }

    /// <summary>
    /// 장비를 장착할 때 사용할 함수
    /// </summary>
    public void EquipItem()
    {
        if (SelectItemId == -1)
            return;

        /*
       var data = 아이템 정보 불러오기(SelectItemId)
       switch(data.id)
      {
     case data.id : 아이템 장착 break;
      } 
     
       */
    }


    /// <summary>
    /// 소모품 사용후 갯수 차감하는 함수
    /// </summary>
    /// <param name="id"></param>
    public void DiscountItemInventory(int id)
    {
        for (int i = 0; i < inventoryInspace.Length; i++)
        {

            if (inventoryInspace[i].GetItemId() == id)
            {
                inventoryInspace[i].ItemChange(id, inventoryInspace[i].GetSprite(), -1);

                if (inventoryInspace[i].GetItemId() == -1)
                {
                    SelectItemId = -1;
                    // 자세하게 보여주던 설명창 나가기
                }
                break;
            }
        }
    }

    /// <summary>
    /// 아이템 획득시 인벤토리에 넣을 때 내용
    /// </summary>
    /// <param name="id">아이템 ID</param>
    /// <param name="sprite">아이템 스포라이트</param>
    /// <param name="count">얻은 갯수</param>
    public void GetItem(int id, Sprite sprite, int count)
    {

        if (inventoryInspace == null || inventoryInspace.Length == 0) return;
       
        for (int i = 0; i < inventoryInspace.Length; i++)
        {
            if (inventoryInspace[i].GetItemId() == id)
            {
                inventoryInspace[i].ItemChange(id, sprite, count);
                return;
            }
        }

        for (int i = 0; i < inventoryInspace.Length; i++)
        {
            if (inventoryInspace[i].GetItemId() == -1 || inventoryInspace[i].GetSprite() == null)
            {
                inventoryInspace[i].ItemChange(id, sprite, count);

                return;
            }
        }


    }

}

