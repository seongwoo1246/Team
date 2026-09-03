/*
Firebase에 로그인한 사용자의 계정을 삭제하는 기능을 담당하는 스크립트
LoginSystemTest를 통해 Firebase Auth 계정을 삭제하고, UserDataManager를 통해 RTDB에 저장된 사용자 데이터를 삭제
LogingScene이 아닌 LobbyScene에서 계정 삭제 후 로그인 화면으로 이동하도록 구현 예정 중
RTDB : Realtime Database (Firebase)
 */

using Cysharp.Threading.Tasks;
using System.Threading;
using Debug = DebugLogger<AccountDeletionHandler>;

public class AccountDeletionHandler : NonMonoSingleton<AccountDeletionHandler>
{
    private const string LOGIN_SCENE = "LoginScene";

    public async UniTask<bool> ProcessAccountDeletionAsync(CancellationToken ct = default)
    {
        string uid = LoginSystemTest.instance.UserId;
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogError("사용자 ID를 가져올 수 없습니다. 로그인 상태를 확인하세요.");
            return false;
        }

        // 1. RTDB 데이터 먼저 제거 
        bool dbDeleted = await UserDataManager.instance.DeleteUserDataAsync(uid, ct);
        if(!dbDeleted)
        {
            Debug.LogError("사용자 데이터를 삭제하는 데 실패했습니다.");
            return false;
        }

        // 2. Firebase Auth 계정 삭제
        var (authSucces, erroMsg) = await LoginSystemTest.instance.DeleteAccountAsync(ct);
        if(!authSucces)
        {
            Debug.LogError($"계정 삭제에 실패했습니다. 오류 메시지: {erroMsg}");
            return false;
        }

        //
        UserDataManager.instance.ClearLocalData();

        // 로그인 기능과 전체적인 틀을 만들면 해제
        //Debug.Log("계정 삭제가 성공적으로 완료되었습니다. 로그인 화면으로 이동합니다.");
        //UnityEngine.SceneManagement.SceneManager.LoadScene(LOGIN_SCENE);
        return true;
    }
}
