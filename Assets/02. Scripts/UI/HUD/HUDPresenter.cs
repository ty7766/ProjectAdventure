using System;
using System.Collections;
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

    private void Start()
    {
        HandleStageStart();
    }

    private void Update()
    {
        HandleTimerUI();
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
        _hudView.HideHUD();
    }

    private void HandleResumeButtonClicked()
    {
        _stageManager?.ResumeGameSmoothly();
        _hudView.HidePauseMenu();
        _hudView.ShowHUD();
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
        _stageManager?.PauseGameSmoothly(3f);
        if(_hudView != null)
        {
            _hudView.HidePauseMenu();
            _hudView.IsPauseMenuActive = false;
            //TODO : 게임 오버 UI 표시

        }
    }

    private void HandleStageClear()
    {
        _stageManager?.PauseGameSmoothly(3f);
        if (_hudView != null)
        {
            _hudView?.HidePauseMenu();
            _hudView.IsPauseMenuActive = false;
            //TODO : 스테이지 클리어 UI 표시
            UpdateStageObjectText();
            _hudView?.ShowStageObjectView();
            _hudView?.ShowStageStartPanel();
            _hudView?.UpdateStageCountDown("Stage Clear!");
        }
    }

    private void HandleStageStart()
    {
        if (_hudView != null)
        {
            _hudView.HideHUD();
            UpdateStageObjectText();
            _hudView.ShowStageStartPanel();
            _hudView.ShowStageObjectView();
        }

        StartCoroutine(StartCountDown());
    }

    IEnumerator StartCountDown()
    {
        float countdownDuration = 5f;
        float elapsed = 0f;
        while (elapsed < countdownDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float remainingTime = countdownDuration - elapsed;
            _hudView?.UpdateStageCountDown(Mathf.CeilToInt(remainingTime).ToString());
            yield return null;
        }
        _hudView?.HideStageStartPanel();
        _hudView?.HideStageObjectView();
        _hudView?.ShowHUD();
        _stageManager?.StartStage();
    }

    private void HandleTimerUI()
    {
        if (_stageManager != null && _hudView != null && _stageManager.IsTimerRunning)
        {
            float elapsedTime = _stageManager.StageTimer;
            _hudView.UpdateTimerUI(elapsedTime);
        }
    }

    private void UpdateStageObjectText()
    {
        if (_stageManager != null && _hudView != null)
        {
            int idx = 0;
            foreach (StageObject obj in _stageManager.StageObjects)
            {
                switch (obj.stageObjectType)
                {
                    case StageObjectType.TimeLimitClear:
                        TimeSpan timeSpan = TimeSpan.FromSeconds(obj.value);
                        _hudView.UpdateStageObjectUI(idx, $"{timeSpan.Minutes}분 {timeSpan.Seconds}초 이내에 스테이지를 클리어 한다.", obj.isCleared);
                        break;
                    case StageObjectType.NoFallClear:
                        _hudView.UpdateStageObjectUI(idx, "한 번도 세상 밖으로 떨어지지 않고 스테이지를 클리어 한다.", obj.isCleared);
                        break;
                    case StageObjectType.NoDamageClear:
                        _hudView.UpdateStageObjectUI(idx, "한 번도 데미지를 입지 않고 스테이지를 클리어 한다.", obj.isCleared);
                        break;
                    case StageObjectType.RemainHealthClear:
                        _hudView.UpdateStageObjectUI(idx, $"스테이지 클리어 시 체력이 {obj.value} 이상 남아 있어야 한다.", obj.isCleared);
                        break;
                    default:
                        _hudView.UpdateStageObjectUI(idx, "알 수 없는 도전과제", obj.isCleared);
                        CustomDebug.LogError("HUDPresenter : 구현되지 않은 도전과제!", gameObject);
                        break;  
                }
                idx++;
            }
        }
    }
}
