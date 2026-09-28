/*담담자 - 송태훈
로컬 계정 및 서버 연동 계정의 탈퇴 처리를 총괄하는 NonMono 싱글톤 핸들러
로컬 세이브 및 Firebase RTDB 데이터와 Auth 인증 계정을 순차 삭제하고 타이틀(부트스트랩) 씬으로 복귀
 */

using Cysharp.Threading.Tasks;
using UtilDebug = DebugLogger<AccountDeletionHandler>;

public class AccountDeletionHandler : NonMonoSingleton<AccountDeletionHandler>
{
    /// <summary>
    /// 로컬 모드일 때는 PlayerPrefs 및 로컬 세이브 데이터를 삭제
    /// 서버 모드일 때는 RTDB 데이터를 선 삭제한 뒤 Firebase Auth 계정을 삭제 후 세션 초기화
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async UniTask<bool> ProcessAccountDeletionAsync(System.Threading.CancellationToken ct = default)
    {
        // 1. 로컬 분기
        if (UserManager.Instance.IsLocalMode)
        {
            UtilDebug.Log("[AccountDeletionHandler] 로컬 게스트 계정 데이터 삭제 시작");

            // 로컬 PlayerPrefs 삭제 및 메모리 정리 실행
            await UserManager.Instance.DeleteUserDataAsync(UserManager.Instance.LocalGuestUID, ct);
            AuthLoginSystem.Instance.SignOut();
            UnityEngine.PlayerPrefs.SetInt("IS_LOCAL_GUEST_ACTIVE", 0);
            UnityEngine.PlayerPrefs.Save();

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

        // 명시적 로그아웃 및 로컬 세션 플래그 제거
        AuthLoginSystem.Instance.SignOut();
        UnityEngine.PlayerPrefs.SetInt("IS_LOCAL_GUEST_ACTIVE", 0);
        UnityEngine.PlayerPrefs.Save();

        // 3. 로컬 캐시 메모리 제거
        UserManager.Instance.ClearLocalData();

        // 4. Bootstrap 씬으로 전환
        await SceneLoadManager.Instance.LoadSceneFlowAsync(SceneId.BootstrapScene);
        return true;
    }
}
