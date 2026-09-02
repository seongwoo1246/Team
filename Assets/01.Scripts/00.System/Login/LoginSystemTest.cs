using TMPro;
using UnityEngine;

public class LoginSystemTest : MonoBehaviour
{
    public TMP_InputField email;
    public TMP_InputField password;

    public TMP_Text outputText;

    void Start()
    {
        FirebaseAuthTest.instance.Init();
        FirebaseAuthTest.instance.LoginState += OnChangedState;
    }

    private void OnChangedState(bool sign)
    {
        outputText.text = sign ? "로그인 : " : "로그아웃 : ";
        outputText.text += FirebaseAuthTest.instance.UserId;
    }
    public void Create()
    {
        FirebaseAuthTest.instance.Create(email.text, password.text);
    }
    public void Login()
    {
        FirebaseAuthTest.instance.Login(email.text, password.text);
    }
    public void Logout()
    {
        FirebaseAuthTest.instance.LogOut();
    }
}
