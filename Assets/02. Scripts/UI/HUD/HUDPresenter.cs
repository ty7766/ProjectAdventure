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
            _hudView?.UpdateStageCountDownContent("Stage Clear!");
        }
    }

    private void HandleStageStart()
    {
        if (_hudView != null)
        {
            _hudView.HideHUD();
            _hudView.IsPauseMenuActive = false;
            _hudView.HidePauseMenu();
            UpdateStageObjectText();
            _hudView.ShowStageStartPanel();
            _hudView.ShowStageObjectView();
        }

        StartCoroutine(StartCountDown());
    }

    IEnumerator StartCountDown()
    {
        string readyText = "준비하세요!";
        _hudView?.UpdateStageCountDownContent(readyText);
        // 시작할 때 폰트 크기 120에서 80으로 0.3초 동안 줄어드는 느낌 (수치는 형 취향대로 조절해!)
        _hudView?.ApplyStageCountDownAnimation(80f, 1.0f);

        yield return new WaitForSecondsRealtime(1.5f);

        // 3, 2, 1 카운트다운 루프
        for (int i = 3; i >= 1; i--)
        {
            //TODO : 사운드 매니저 통합
            //ex) SoundManager.Instance.PlaySFX("CountdownBeep");

            _hudView?.ApplyStageCountDownAnimation(128f, 0.5f);
            _hudView?.UpdateStageCountDownContent(i.ToString());
            yield return new WaitForSecondsRealtime(.5f);

            _hudView?.ApplyStageCountDownAnimation(100f, 0.5f);

            yield return new WaitForSecondsRealtime(.5f);
        }

        // "START!" 혹은 "GO!" 연출 (필요 없으면 생략 가능)
        _hudView?.UpdateStageCountDownContent("GO!");
        _hudView?.ApplyStageCountDownAnimation(120f, 0.2f);
        yield return new WaitForSecondsRealtime(0.5f);

        // 기존 종료 로직 수행
        _hudView?.HideStageStartPanel();
        _hudView?.HideStageObjectView();
        _hudView?.ShowHUD();

        if (_hudView != null)
        {
            _hudView.IsPauseMenuActive = true;
        }

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
