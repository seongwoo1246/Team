using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 인벤토리 미리 만들어서 넣어둔다고 해서 만든 슬롯 스크립트, 
/// </summary>
public class InventorySlot : MonoBehaviour
{
    // 아이템 UI 여기서는 버튼으로 사용될 것으로 예상하고 이미지로 만듬
    [SerializeField] Image image;
    //소모품일 경우 숫자를 적어서 사용할 때 마다 한개씩 줄어드는 식으로 활용예정 / 소모품 아니면 빈칸으로 두면 됨
    [SerializeField] TextMeshProUGUI CountText;

    private int ItemID = -1;
    private int Count = 0;

    //아이템이 빈 공간이거나 아이템이 바뀔 때 이미지의 스프라이트를 바뀌주는 식으로 만듬
    public Sprite GetSprite() => image.sprite;
   //여기서는 아이템 아이디를 가지고 옴
    public int GetItemId() => ItemID;

    private void Start()
    {     
        Button button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OnSlotClicked);
    }

    /// <summary>
    /// 인벤토리 칸과 아이템을 바꿔주는 함수
    /// </summary>
    /// <param name="id">아이템 아이디</param>
    /// <param name="sprite">아이템 스프라이트</param>
    /// <param name="count">아이템의 갯수</param>
    public void ItemChange(int id, Sprite sprite, int count)
    {
        Count += count;
        if (Count <= 0)
        {
            ClearSpace();
            return;
        }
        ItemID = id;
        image.sprite = sprite;
        CountText.text = Count.ToString();

    }

    /// <summary>
    /// 인벤토리칸 초기화 함수로 빈 흰 공간으로 만들 예정
    /// </summary>
    public void ClearSpace()
    {
        ItemID = -1;
        image.sprite = null;
        image.color = Color.white;
        CountText.text = "";
        Count = 0;
    }

    /// <summary>
    /// 인벤토리 칸을 클릭했을 때 하는 함수 
    /// </summary>
    public void OnSlotClicked()
    {
        if (GetSprite() == null)
        {
            return;
        }
        if (ItemID == -1)
            return;
       //여기서 아이템 ID를 찾아서 들고 오기
       //UI 아이템 상세 설명 창 같은 느낌 보여주기
    }
}
