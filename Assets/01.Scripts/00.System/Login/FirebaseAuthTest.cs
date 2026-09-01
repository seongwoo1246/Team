using Firebase.Auth;
using System;
using Debug = DebugLogger<FirebaseAuthTest>;


public class FirebaseAuthTest : NonMonoSingleton<FirebaseAuthTest>
{
    private FirebaseAuth auth;  // 로그인 -> 회원가입 등에 사용
    private FirebaseUser user;  // 인증이 완료된 유저 정보

    public string UserId => user.UserId;
    public Action<bool> LoginState;

    public override void Init()
    {
        base.Init();
        auth = FirebaseAuth.DefaultInstance;

        // static으로 인한 user 메모리 저장을 해제하는 임시 처리
        if(auth.CurrentUser != null)
        {
            LogOut();
        }

        auth.StateChanged += OnChanged;
    }

    private void OnChanged(object sender, System.EventArgs e)
    {
        if (auth.CurrentUser != user)
        {
            bool signed = (auth.CurrentUser != user && auth.CurrentUser != null);
            if (!signed && user != null)
            {
                Debug.Log("로그아웃");
                LoginState?.Invoke(false);
            }

            user = auth.CurrentUser;
            if(signed)
            {
                LoginState?.Invoke(true);
                Debug.Log("로그인");
            }
        }
    }

    public void Create(string email, string password)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("회원가입 취소");
                return;
            }

            if (task.IsFaulted)
            {
                // 회원가입 실패 이유 => 이메일이 비정상 / 비밀번호 간단 / 이미 가입된 이메일 등등...
                Debug.LogError("회원가입 실패");
                return;
            }

            AuthResult authResult = task.Result;
            FirebaseUser newUser = authResult.User;
            Debug.LogWarning("회원가입 안료");
        });
    }


    public void Login(string email, string password)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("로그인 취소");
                return;
            }

            if (task.IsFaulted)
            {
                // 로그인 실패 이유 => 이메일이 비정상 / 비밀번호 간단 / 이미 가입된 이메일 등등...
                Debug.LogError("로그인 실패");
                return;
            }

            AuthResult authResult = task.Result;
            FirebaseUser newUser = authResult.User;
            Debug.LogWarning("로그인 안료");
        });
    }

    public void LogOut()

    {
        auth.SignOut();
    }



}
