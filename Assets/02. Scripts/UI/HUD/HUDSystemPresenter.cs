using GameManager.Singleton;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HUDSystemPresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HUDView _hudView;
    [SerializeField] private StageManager _stageManager;

    private void Awake()
    {
        if (_hudView == null) _hudView = GetComponent<HUDView>();
        SubscribeButtonEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeButtonEvents();
    }

    /// <summary>
    /// 스테이지 전환 시 StageManager 레퍼런스를 재연결합니다.
    /// </summary>
    /// <param name="stageManager">현재 활성화된 스테이지의 StageManager</param>
    public void Setup(StageManager stageManager)
    {
        _stageManager = stageManager;
    }

    private void SubscribeButtonEvents()
    {
        if (_hudView == null) return;
        _hudView.OnPauseButtonClicked += HandlePause;
        _hudView.OnResumeButtonClicked += HandleResume;
        _hudView.OnReturnToMainMenuButtonClicked += HandleReturnToMainMenu;
        _hudView.OnQuitGameButtonClicked += HandleQuitGame;
        _hudView.OnRetryButtonClicked += HandleRetryStage;
        _hudView.OnGoToNextStageButtonClicked += HandleGoToNextStage;
    }

    private void UnsubscribeButtonEvents()
    {
        if (_hudView == null) return;
        _hudView.OnPauseButtonClicked -= HandlePause;
        _hudView.OnResumeButtonClicked -= HandleResume;
        _hudView.OnReturnToMainMenuButtonClicked -= HandleReturnToMainMenu;
        _hudView.OnQuitGameButtonClicked -= HandleQuitGame;
        _hudView.OnRetryButtonClicked -= HandleRetryStage;
        _hudView.OnGoToNextStageButtonClicked -= HandleGoToNextStage;
    }

    private void HandlePause()
    {
        _stageManager?.PauseGameSmoothly();
        _hudView.ShowPauseMenu();
        _hudView.HideHUD();
    }

    private void HandleResume()
    {
        _stageManager?.ResumeGameSmoothly();
        _hudView.HidePauseMenu();
        _hudView.ShowHUD();
    }

    private void HandleReturnToMainMenu()
    {
        SceneManager.LoadScene("dev-title");
    }

    private void HandleQuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    private void HandleRetryStage()
    {
        StageLoader.Instance.ReloadCurrentStage();
    }

    private void HandleGoToNextStage()
    {
        int nextStageNumber = GameSaveManager.Instance.CurrentStageNumber + 1;
        if (GameSaveManager.Instance.CheckStageUnlockRequirement(nextStageNumber) == true)
        {
            GameSaveManager.Instance.LoadStage(nextStageNumber);
        }
    }
}