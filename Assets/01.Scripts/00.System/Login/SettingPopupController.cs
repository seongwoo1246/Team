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

    #region 담당자- 정성우 소리 관련 작성

    [Header("Ui 버튼들")]
    [SerializeField] private GameObject bgmOnBtn;
    [SerializeField] private GameObject bgmOffBtn;
    [SerializeField] private GameObject sfxOnBtn;
    [SerializeField] private GameObject sfxOffBtn;
    [SerializeField] private Slider bgmvolume;
    [SerializeField] private Slider sfxVolume;


    

    #endregion

    private bool _isProcessing = false;

    private void Awake()
    {
        //슬라이더 값을 음악 소리로 넘겨주기
        if(bgmvolume != null)
        {
            bgmvolume.onValueChanged.AddListener(SoundManager.Instance.SetBGMVolume);
        }
        if(sfxVolume != null)
        {
            sfxVolume.onValueChanged.AddListener(SoundManager.Instance.SetSFXVolume);
        }



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

    #region 담당자 - 정성우 소리 관련 함수들
    public void MuteOnOffBGM()
    {
        if(SoundManager.Instance != null)
        {
            bool isMuted = !SoundManager.Instance.FadeOutSource.mute;

            SoundManager.Instance.FadeInSource.mute = isMuted;
            SoundManager.Instance.FadeOutSource.mute = isMuted;

            bgmOnBtn.gameObject.SetActive(!isMuted);
            bgmOffBtn.gameObject.SetActive(isMuted);

            PlayerPrefs.SetInt("BGMMute", isMuted ? 1 : 0);
            
        }
       

    }
    public void MuteOnOffSFX()
    {
        if (SoundManager.Instance != null)
        {
            bool isMuted = !SoundManager.Instance.sfxSource.mute;

            SoundManager.Instance.sfxSource.mute = isMuted;

            sfxOnBtn.gameObject.SetActive(!isMuted);
            sfxOffBtn.gameObject.SetActive(isMuted);

            PlayerPrefs.SetInt("SFXMute", isMuted ? 1 : 0);
          
        }
           
    }

    #endregion

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

        PlayerPrefs.Save();
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

            // 1. 현재 데이터 최종 플러시
            if (GameManager.Instance != null)
            {
                await GameManager.Instance.FlushGameDataAsync();
            }

            // 2. 로컬 게스트 세션 플래그 해제 (로그인 화면이 다시 뜨게 함)
            PlayerPrefs.SetInt("IS_LOCAL_GUEST_ACTIVE", 0);
            PlayerPrefs.Save();

            // 3. Firebase Auth 로그아웃 실행 (로컬 모드여도 안전하게 호출 가능)
            AuthLoginSystem.Instance.SignOut();

            // 4. 런타임 유저 정보 초기화
            UserManager.Instance.IsLocalMode = false;
            UserManager.Instance.ClearLocalData();

            ClosePopup();

            // 5. 초기 타이틀 씬으로 복귀
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