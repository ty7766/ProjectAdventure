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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void HandleGoToNextStage()
    {
        if(GameSaveManager.Instance == null)
        {
            return;
        }

        int nextStageNumber = GameSaveManager.Instance.CurrentStageNumber + 1;
        if (GameSaveManager.Instance?.CheckStageUnlockRequirement(nextStageNumber) == true)
        {
            GameSaveManager.Instance?.LoadStage(nextStageNumber);
        }
    }
}