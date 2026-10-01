using GameManager.Singleton;
using UnityEngine;
using System.Collections;

public class HUDSystemPresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HUDView _hudView;
    [SerializeField] private StageManager _stageManager;
    [SerializeField] private MapManager _mapManager;

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

    /// <summary>
    /// 스테이지 전환 시 MapManager 레퍼런스를 재연결합니다.
    /// </summary>
    /// <param name="mapManager">현재 활성화된 스테이지의 MapManager</param>
    public void Setup(MapManager mapManager)
    {
        _mapManager = mapManager;
        if (_mapManager != null)
        {
            _hudView.StartMapSwapCooldownWatch(() => _mapManager.MapChangeCooldownRemaining01);
        }
        else
        {
            _hudView.StopMapSwapCooldownWatch();
        }
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
        _hudView.OnMapSelectLeftClicked += HandleMapSelectLeft;
        _hudView.OnMapSelectRightClicked += HandleMapSelectRight;
        _hudView.OnMapSwapClicked += HandleMapSwap;
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
        _hudView.OnMapSelectLeftClicked -= HandleMapSelectLeft;
        _hudView.OnMapSelectRightClicked -= HandleMapSelectRight;
        _hudView.OnMapSwapClicked -= HandleMapSwap;
    }

    private void HandleMapSelectLeft()
    {
        _mapManager?.SelectMap(-1);
    }

    private void HandleMapSelectRight()
    {
        _mapManager?.SelectMap(1);
    }

    private void HandleMapSwap()
    {
        _mapManager?.SwapMap(1);
    }

    private void HandlePause()
    {
        // 키보드 일시정지(HUDView.HandleInput)와 동일하게 튜토리얼/전환 연출 중에는 일시정지를 막는다
        if (TutorialManager.IsActive || SceneTransitionManager.IsBusy)
        {
            return;
        }
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
        LoadTitleScene();
    }

    /// <summary>
    /// 플레이어를 중심으로 닫히는 전환 후 타이틀 씬으로 이동합니다. (timeScale은 덮인 뒤 복구)
    /// </summary>
    private static void LoadTitleScene()
    {
        SceneTransitionManager.TryLoadScene("dev-title", SceneTransitionOptions.ToTitle, () => Time.timeScale = 1f);
    }

    private void HandleQuitGame()
    {
        if (GlobalUICanvasView.Instance != null && GlobalUICanvasView.Instance.PopupPresenter != null)
        {
            GlobalUICanvasView.Instance.PopupPresenter.ShowPopup(
                "경고",
                "정말 게임을 종료하시겠습니까?",
                ("확인", () =>
                {
                    GlobalUICanvasView.Instance.PopupPresenter.HidePopup();
                    PerformQuit();
                }
            ),
                ("취소", () => GlobalUICanvasView.Instance.PopupPresenter.HidePopup())
            );
        }
        else
        {
            PerformQuit();
        }
    }

    private void PerformQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void HandleRetryStage()
    {
        // 같은 자리에서 다시 시작하므로 짧은 페이드로 덮고, 덮인 동안 상태를 되돌린다
        SceneTransitionManager.TryRun(SceneTransitionOptions.Retry, () =>
        {
            Time.timeScale = 1f;
            StageLoader.Instance?.ReloadCurrentStage();
        });
    }

    private void HandleGoToNextStage()
    {
        if (GameSaveManager.Instance == null) return;

        int currentStageNumber = GameSaveManager.Instance.CurrentStageNumber;
        int totalStages = GameSaveManager.Instance.GetTotalStageNumber();

        if (currentStageNumber >= totalStages)
        {
            if (GlobalUICanvasView.Instance != null && GlobalUICanvasView.Instance.PopupPresenter != null)
            {
                GlobalUICanvasView.Instance.PopupPresenter.ShowPopup(
                    "축하합니다!",
                    "모든 스테이지를 클리어하셨습니다!",
                    ("타이틀로", () => {
                        if (GlobalUICanvasView.Instance != null && GlobalUICanvasView.Instance.PopupPresenter != null)
                        {
                            GlobalUICanvasView.Instance.PopupPresenter.HidePopup();
                        }
                        LoadTitleScene();
                    })
                );
            }
            else
            {
                CustomDebug.LogWarning("[HUDSystemPresenter] GlobalUICanvasView.Instance or PopupPresenter is not initialized.");
            }
            return;
        }

        int nextStageNumber = currentStageNumber + 1;
        if (GameSaveManager.Instance.CheckStageUnlockRequirement(nextStageNumber))
        {
            GameSaveManager.Instance.LoadStage(nextStageNumber);
        }
        else
        {
            if (GlobalUICanvasView.Instance != null && GlobalUICanvasView.Instance.PopupPresenter != null)
            {
                GlobalUICanvasView.Instance.PopupPresenter.ShowPopup(
                    "스테이지 미해금",
                    "다음 스테이지는 아직 해금되지 않았습니다.",
                    ("타이틀로", () => {
                        if (GlobalUICanvasView.Instance != null && GlobalUICanvasView.Instance.PopupPresenter != null)
                        {
                            GlobalUICanvasView.Instance.PopupPresenter.HidePopup();
                        }
                        LoadTitleScene();
                    })
                );
            }
            else
            {
                CustomDebug.LogWarning("[HUDSystemPresenter] GlobalUICanvasView.Instance or PopupPresenter is not initialized.");
            }
        }
    }
}