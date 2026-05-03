using GameManager.Singleton;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

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
        Time.timeScale = 1f;
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
        Time.timeScale = 1f;
        StageLoader.Instance?.ReloadCurrentStage();
    }

    private void HandleGoToNextStage()
    {
        if (GameSaveManager.Instance == null) return;

        int currentStageNumber = GameSaveManager.Instance.CurrentStageNumber;
        int totalStages = GameSaveManager.Instance.GetTotalStageNumber();

        if (currentStageNumber >= totalStages)
        {
            GlobalUICanvasView.Instance.Presenter.ShowPopup(
                "축하합니다!",
                "모든 스테이지를 클리어하셨습니다!",
                ("타이틀로", () => {
                    Time.timeScale = 1f;
                    GlobalUICanvasView.Instance.Presenter.HidePopup();
                    SceneManager.LoadScene("dev-title");
                })
            );
            return;
        }

        int nextStageNumber = currentStageNumber + 1;
        if (GameSaveManager.Instance.CheckStageUnlockRequirement(nextStageNumber))
        {
            GameSaveManager.Instance.LoadStage(nextStageNumber);
        }
        else
        {
            GlobalUICanvasView.Instance.Presenter.ShowPopup(
                "스테이지 미해금",
                "다음 스테이지는 아직 해금되지 않았습니다.",
                ("타이틀로", () => {
                    Time.timeScale = 1f;
                    GlobalUICanvasView.Instance.Presenter.HidePopup();
                    SceneManager.LoadScene("dev-title");
                })
            );
        }
    }
}