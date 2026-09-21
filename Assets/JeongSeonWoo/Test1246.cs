using UnityEngine;


public class Test1246 : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        MailBoxManager.Instance.AddMail("테스트", "테스트로 보내고 있는 내용이니 걱정 마세요", 1);
        Debug.Log("dy");
    }
    // Update is called once per frame
    void Update()
    {
      
    }
}
