/*담담자 - 송태훈
Firebase에 로그인한 사용자의 계정을 삭제하는 기능을 담당하는 스크립트
LoginSystemTest를 통해 Firebase Auth 계정을 삭제하고, UserDataManager를 통해 RTDB에 저장된 사용자 데이터를 삭제
LogingScene이 아닌 LobbyScene에서 계정 삭제 후 로그인 화면으로 이동하도록 구현 예정 중
RTDB : Realtime Database (Firebase)
 */

using Cysharp.Threading.Tasks;
using System.Threading;
using UtilDebug = DebugLogger<AccountDeletionHandler>;

public class AccountDeletionHandler : NonMonoSingleton<AccountDeletionHandler>
{
    public async UniTask<bool> ProcessAccountDeletionAsync(CancellationToken ct = default)
    {
        // 1. 로컬 분기
        if (UserManager.Instance.IsLocalMode)
        {
            UtilDebug.Log("[AccountDeletionHandler] 로컬 게스트 계정 데이터 삭제 시작");

            // 로컬 PlayerPrefs 삭제 및 메모리 정리 실행
            await UserManager.Instance.DeleteUserDataAsync(UserManager.Instance.LocalGuestUID, ct);

            // 타이틀 씬으로 복귀
            await SceneLoadManager.Instance.LoadSceneFlowAsync(SceneId.BootstrapScene);
            return true;
        }

        // 2. 서버 분기
        string uid = AuthLoginSystem.Instance.UserId;
        if (string.IsNullOrEmpty(uid))
        {
            UtilDebug.LogError("사용자 ID를 가져올 수 없습니다. 로그인 상태를 확인하세요.");
            return false;
        }

        // 1. RTDB 데이터 먼저 제거 
        bool dbDeleted = await UserManager.Instance.DeleteUserDataAsync(uid, ct);
        if(!dbDeleted)
        {
            UtilDebug.LogError("사용자 데이터를 삭제하는 데 실패했습니다.");
            return false;
        }

        // 2. Firebase Auth 계정 삭제
        var (authSucces, erroMsg) = await AuthLoginSystem.Instance.DeleteAccountAsync(ct);
        if(!authSucces)
        {
            UtilDebug.LogError($"계정 삭제에 실패했습니다. 오류 메시지: {erroMsg}");
            return false;
        }

        // 3. 로컬 캐시 메모리 제거
        UserManager.Instance.ClearLocalData();

        // 로그인 기능과 전체적인 틀을 만들면 해제
        await SceneLoadManager.Instance.LoadSceneFlowAsync(SceneId.BootstrapScene);
        return true;
    }
}
