using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UtilDebug = DebugLogger<SettingsPopupController>;

public class SettingsPopupController : MonoBehaviour
{
    [Header("패널 루트")]
    [SerializeField] private GameObject popupRoot; // Settings 또는 Popup 오브젝트

    [Header("UID 표시 및 복사")]
    [SerializeField] private TextMeshProUGUI uidText;
    [SerializeField] private Button copyUidButton;

    [Header("1번 그룹 버튼 (로그아웃 / 전체 저장)")]
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button saveAllButton;

    [Header("2번 그룹 버튼 (계정 삭제 / 게임 종료)")]
    [SerializeField] private Button accountDeleteButton;
    [SerializeField] private Button quitGameButton;

    [Header("기본 닫기 버튼")]
    [SerializeField] private Button closeButton;

    [Header("계정 삭제 재확인 팝업 (선택 사항)")]
    [SerializeField] private GameObject deleteConfirmPanel;
    [SerializeField] private Button deleteConfirmYesButton;
    [SerializeField] private Button deleteConfirmNoButton;

    private bool _isProcessing = false;

    private void Awake()
    {
        // 닫기 버튼 바인딩
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePopup);
        }

        // UID 복사 버튼 바인딩
        if (copyUidButton != null)
        {
            copyUidButton.onClick.AddListener(CopyUidToClipboard);
        }

        // 로그아웃 버튼 바인딩
        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(() => OnClickLogoutAsync().Forget());
        }

        // 전체 저장 버튼 바인딩
        if (saveAllButton != null)
        {
            saveAllButton.onClick.AddListener(() => OnClickSaveAllAsync().Forget());
        }

        // 계정 삭제 버튼 바인딩
        if (accountDeleteButton != null)
        {
            accountDeleteButton.onClick.AddListener(OnClickAccountDelete);
        }

        // 게임 종료 버튼 바인딩
        if (quitGameButton != null)
        {
            quitGameButton.onClick.AddListener(() => OnClickQuitGameAsync().Forget());
        }

        // 삭제 재확인 창 바인딩
        if (deleteConfirmYesButton != null)
        {
            deleteConfirmYesButton.onClick.AddListener(() => OnConfirmDeleteAccountAsync().Forget());
        }

        if (deleteConfirmNoButton != null)
        {
            deleteConfirmNoButton.onClick.AddListener(() =>
            {
                if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(false);
            });
        }
    }

    /// <summary>
    /// 로비의 [Setting] 버튼 OnClick에 연결하여 팝업 오픈
    /// </summary>
    public void OpenPopup()
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }

        RefreshUidDisplay();

        if (deleteConfirmPanel != null)
        {
            deleteConfirmPanel.SetActive(false);
        }
        Time.timeScale = 0;
    }

    public void ClosePopup()
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
        Time.timeScale = 1;
    }

    private void RefreshUidDisplay()
    {
        string uid = AuthLoginSystem.Instance.UserId;
        if (string.IsNullOrEmpty(uid) && UserManager.Instance.CurrentUser != null)
        {
            uid = UserManager.Instance.CurrentUser.UID;
        }

        if (uidText != null)
        {
            uidText.text = string.IsNullOrEmpty(uid) ? "Player ID: None" : $"Player ID #{uid}";
        }
    }

    private void CopyUidToClipboard()
    {
        string uid = AuthLoginSystem.Instance.UserId;
        if (string.IsNullOrEmpty(uid) && UserManager.Instance.CurrentUser != null)
        {
            uid = UserManager.Instance.CurrentUser.UID;
        }

        if (!string.IsNullOrEmpty(uid))
        {
            GUIUtility.systemCopyBuffer = uid;
            UtilDebug.Log($"UID 클립보드 복사 완료: {uid}");
        }
    }

    #region 전체 저장 (Save All) 로직
    private async UniTaskVoid OnClickSaveAllAsync()
    {
        if (_isProcessing) return;
        _isProcessing = true;

        try
        {
            UtilDebug.Log("전체 데이터 동기화 및 저장 시작...");

            // 1. ISyncable 등록된 매니저들(인벤토리 등) 메모리 동기화 및 서버 플러시
            if (GameManager.Instance != null)
            {
                await GameManager.Instance.FlushGameDataAsync();
            }

            UtilDebug.Log("전체 데이터 저장 성공");
        }
        catch (System.Exception ex)
        {
            UtilDebug.LogError($"전체 저장 중 예외 발생: {ex.Message}");
        }
        finally
        {
            _isProcessing = false;
        }
    }
    #endregion

    #region 로그아웃 로직
    private async UniTaskVoid OnClickLogoutAsync()
    {
        if (_isProcessing) return;
        _isProcessing = true;

        try
        {
            UtilDebug.Log("로그아웃 프로세스 시작");

            // 1. 현재 데이터 최종 플러시 (유실 방지)
            if (GameManager.Instance != null)
            {
                await GameManager.Instance.FlushGameDataAsync();
            }

            // 2. Firebase Auth 로그아웃 실행
            AuthLoginSystem.Instance.SignOut();

            // 3. 로컬 런타임 유저 정보 초기화
            UserManager.Instance.ClearLocalData();

            ClosePopup();

            // 4. 로그인/초기 씬으로 이동
            await SceneLoadManager.Instance.LoadSceneFlowAsync(SceneId.BootstrapScene);
        }
        catch (System.Exception ex)
        {
            UtilDebug.LogError($"로그아웃 중 예외 발생: {ex.Message}");
        }
        finally
        {
            _isProcessing = false;
        }
    }
    #endregion

    #region 계정 삭제 로직
    private void OnClickAccountDelete()
    {
        if (deleteConfirmPanel != null)
        {
            deleteConfirmPanel.SetActive(true);
        }
        else
        {
            OnConfirmDeleteAccountAsync().Forget();
        }
    }

    private async UniTaskVoid OnConfirmDeleteAccountAsync()
    {
        if (_isProcessing) return;
        _isProcessing = true;

        var ct = this.destroyCancellationToken;

        try
        {
            UtilDebug.Log("계정 삭제 프로세스 시작");

            bool success = await AccountDeletionHandler.Instance.ProcessAccountDeletionAsync(ct);
            if (success)
            {
                ClosePopup();
                UtilDebug.Log("계정 삭제 완료 및 초기 씬으로 복귀");
            }
            else
            {
                UtilDebug.LogError("계정 삭제 처리에 실패했습니다.");
            }
        }
        catch (System.Exception ex)
        {
            UtilDebug.LogError($"계정 삭제 중 오류 발생: {ex.Message}");
        }
        finally
        {
            _isProcessing = false;
        }
    }
    #endregion

    #region 게임 종료 로직
    private async UniTaskVoid OnClickQuitGameAsync()
    {
        if (_isProcessing) return;
        _isProcessing = true;


        try
        {
            UtilDebug.Log("게임 종료 전 안전 저장 수행...");

            // 종료 전 데이터 저장 플러시
            if (GameManager.Instance != null)
            {
                await GameManager.Instance.FlushGameDataAsync();
            }
        }
        catch (System.Exception ex)
        {
            UtilDebug.LogError($"게임 종료 저장 중 예외 발생: {ex.Message}");
        }
        finally
        {
            _isProcessing = false;

            UtilDebug.Log("애플리케이션 종료");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
    #endregion
}