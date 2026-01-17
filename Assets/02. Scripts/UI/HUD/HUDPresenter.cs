using UnityEngine;

[RequireComponent(typeof(HUDView))]
public class HUDPresenter : MonoBehaviour
{
    //--- Settings ---//
    [Header("References")]
    [SerializeField] private PlayerProperties _playerModel;
    [SerializeField] private PlayerEffectController _effectController;
    [SerializeField] private StageManager _stageManager;
    private HUDView _hudView;

    //--- Unity Methods ---//
    private void Awake()
    {
        GetRequiredComponents();
        SubscribeEventHandlers();
    }

    private void OnDestroy()
    {
        UnsubscribeEventHandlers();
    }

    //--- Private Methods ---//
    private void GetRequiredComponents()
    {
        _hudView = GetComponent<HUDView>();
    }

    private void SubscribeEventHandlers()
    {
        if (_playerModel != null)
        {
            _hudView.UpdateHealthUI(_playerModel.Health);
            _playerModel.OnHealthChanged += HandleHealthChanged;
            _playerModel.OnPlayerDeath += HandlePlayerDeath;
        }
        if (_stageManager != null)
        {
            _stageManager.OnGemCountChanged += HandleGemUpdate;
            _stageManager.OnStageCleared += HandleStageClear;
        }
        if (_effectController != null)
        {
            _effectController.OnBuffListChanged += HandleBuffChange;
        }
        if (_hudView != null)
        {
            _hudView.OnPauseButtonClicked += HandlePauseButtonClicked;
            _hudView.OnResumeButtonClicked += HandleResumeButtonClicked;
            _hudView.OnReturnToMainMenuButtonClicked += HandleReturnToMainMenuButtonClicked;
            _hudView.OnQuitGameButtonClicked += HandleQuitGameButtonClicked;
        }
    }

    private void UnsubscribeEventHandlers()
    {
        if (_playerModel != null)
        {
            _playerModel.OnHealthChanged -= HandleHealthChanged;
            _playerModel.OnPlayerDeath -= HandlePlayerDeath;
        }
        if (_stageManager != null)
        {
            _stageManager.OnGemCountChanged -= HandleGemUpdate;
            _stageManager.OnStageCleared -= HandleStageClear;
        }
        if (_effectController != null)
        {
            _effectController.OnBuffListChanged -= HandleBuffChange;
        }
        if (_hudView != null)
        {
            _hudView.OnPauseButtonClicked -= HandlePauseButtonClicked;
            _hudView.OnResumeButtonClicked -= HandleResumeButtonClicked;
            _hudView.OnReturnToMainMenuButtonClicked -= HandleReturnToMainMenuButtonClicked;
            _hudView.OnQuitGameButtonClicked -= HandleQuitGameButtonClicked;
        }
    }

    private void HandleHealthChanged(int newHealth)
    {
        _hudView.UpdateHealthUI(newHealth);
    }

    private void HandleGemUpdate(int current, int total)
    {
        _hudView.UpdateGemUI(current, total);
    }

    private void HandleBuffChange(System.Collections.Generic.List<ActiveEffect> effects)
    {
        _hudView.RefreshBuffs(effects);
    }

    private void HandlePauseButtonClicked()
    {
        _stageManager?.PauseGameSmoothly();
        _hudView.ShowPauseMenu();
    }

    private void HandleResumeButtonClicked()
    {
        _stageManager?.ResumeGameSmoothly();
        _hudView.HidePauseMenu();
    }

    private void HandleReturnToMainMenuButtonClicked()
    {
        //TODO : 메인메뉴로 돌아가기
    }

    private void HandleQuitGameButtonClicked()
    {
        //TODO : 게임 종료
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    private void HandlePlayerDeath()
    {
        _stageManager?.PauseGameSmoothly(1f);
        if(_hudView != null)
        {
            _hudView.IsPauseMenuActive = false;
            //TODO : 게임 오버 UI 표시

        }
    }

    private void HandleStageClear()
    {
        _stageManager?.PauseGameSmoothly(1f);
        if (_hudView != null)
        {
            _hudView.IsPauseMenuActive = false;
            //TODO : 스테이지 클리어 UI 표시

        }
    }
}
