using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using GameManager.Singleton;

[System.Serializable]
public struct StageMissionData
{
    public StageObjectType Type;
    [TextArea] public string FormatText; // 인스펙터에서 줄바꿈 편하게
}

public class HUDFlowPresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private HUDView _hudView;
    [SerializeField]
    private StageManager _stageManager;
    [SerializeField]
    private PlayerProperties _playerModel;
    [SerializeField]
    private TutorialManager _tutorialManager;
    [SerializeField]
    private StageObjectObserver _stageObjectObserver;

    [Header("Text Strings")]
    [SerializeField]
    private List<StageMissionData> _missionDataList;
    [SerializeField]
    private string _stageReadyString = "준비하세요!";
    [SerializeField]
    private string _stageGoString = "GO!";

    public static Dictionary<StageObjectType, string> MissionTextDict = new Dictionary<StageObjectType, string>();

    private Coroutine _countDownCoroutine;
    private Coroutine _deathSequenceCoroutine;
    private bool _isSetupCalled = false;
    


    private void Awake()
    {
        foreach(var data in _missionDataList)
        {
            if (!MissionTextDict.ContainsKey(data.Type))
            {
                MissionTextDict.Add(data.Type, data.FormatText);
            }
        }

        if (_hudView == null) _hudView = GetComponent<HUDView>();
        if (_stageManager != null) _stageManager.OnStageCleared += HandleStageClear;
        if (_playerModel != null) _playerModel.OnPlayerDeath += HandlePlayerDeath;
        _hudView.SetTouchControlsVisible(PlatformCapability.UseTouchUI);
    }

    private void Start()
    {
        if (!_isSetupCalled)
        {
            HandleStageStart();
        }
    }

    private void OnDestroy()
    {
        if (_stageManager != null) _stageManager.OnStageCleared -= HandleStageClear;
        if (_playerModel != null) _playerModel.OnPlayerDeath -= HandlePlayerDeath;
    }

    /// <summary>
    /// 스테이지 전환 시 StageManager와 PlayerProperties 레퍼런스를 재연결하고 스테이지 시작 시퀀스를 실행합니다.
    /// </summary>
    /// <param name="stageManager">현재 활성화된 스테이지의 StageManager</param>
    /// <param name="playerModel">플레이어 스탯 및 이벤트 제공 컴포넌트</param>
    public void Setup(StageManager stageManager, PlayerProperties playerModel)
    {
        // 기존 구독 해제
        if (_stageManager != null) _stageManager.OnStageCleared -= HandleStageClear;
        if (_playerModel != null) _playerModel.OnPlayerDeath -= HandlePlayerDeath;

        _stageManager = stageManager;
        _playerModel = playerModel;

        // 새 구독
        if (_stageManager != null) _stageManager.OnStageCleared += HandleStageClear;
        if (_playerModel != null) _playerModel.OnPlayerDeath += HandlePlayerDeath;

        _stageObjectObserver?.Setup(_stageManager);

        // UI 갱신
        _isSetupCalled = true;
        HandleStageStart();
    }

    // --- Start Sequence ---
    private void HandleStageStart()
    {
        if (_deathSequenceCoroutine != null)
        {
            StopCoroutine(_deathSequenceCoroutine);
            _deathSequenceCoroutine = null;
        }
        NotificationPresenter.Reset();

        // 튜토리얼 도중 재시작/스테이지 교체된 경우 이전 튜토리얼 세션을 정리한다 (카운트다운 후 처음부터 다시 시작)
        if (_tutorialManager != null)
        {
            _tutorialManager.AbortTutorial();
        }

        SoundManager.Instance?.PlayBGM(SoundType.None);

        _hudView.HideStageFailPanel();
        _hudView.HideHUD();
        _hudView.HidePauseMenu();
        _hudView.HideStageClearPanel();
        _hudView.IsPauseMenuActive = false;

        UpdateStageObjectText();
        _hudView.ShowStageStartPanel();

        if (_countDownCoroutine != null)
        {
            StopCoroutine(_countDownCoroutine);
        }
        _countDownCoroutine = StartCoroutine(StartCountDown());
    }

    private IEnumerator StartCountDown()
    {
        // 전환 연출이 화면을 다 걷은 뒤에 카운트다운을 시작한다 (덮인 동안 지나가 버리지 않도록)
        while (SceneTransitionManager.IsBusy)
        {
            yield return null;
        }

        _hudView.PlayCountdownReady(_stageReadyString);
        yield return new WaitForSecondsRealtime(1.5f);

        for (int i = 3; i >= 1; i--)
        {
            SoundManager.Instance?.PlaySFX(SoundType.SFX_GameStartCountdown);

            _hudView.PlayCountdownTick(i.ToString());
            yield return new WaitForSecondsRealtime(1.0f);
        }

        SoundManager.Instance?.PlaySFX(SoundType.SFX_GameStart);
        SoundManager.Instance?.PlayBGM(SoundType.BGM_BackGroundMusic);

        _hudView.PlayCountdownGo(_stageGoString);
        yield return new WaitForSecondsRealtime(0.5f);

        _hudView.HideStageStartPanel();
        _hudView.ShowHUD();
        _hudView.IsPauseMenuActive = true;

        //처음 플레이 할 시 게임 시작 전 튜토리얼 보여주기 
        if (_tutorialManager != null && _tutorialManager.ShouldShowTutorial())
        {
            _tutorialManager.StartTutorial(_stageManager, () => _stageManager?.StartStage());
        }
        else
        {
            _stageManager?.StartStage();
        }
    }

    // --- Death Sequence ---
    private void HandlePlayerDeath()
    {
        if (_deathSequenceCoroutine != null) StopCoroutine(_deathSequenceCoroutine);
        _deathSequenceCoroutine = StartCoroutine(PlayerDeathSequence());
    }

    private IEnumerator PlayerDeathSequence()
    {
        _stageManager?.PauseGameSmoothly(1.5f);
        yield return new WaitForSecondsRealtime(1.5f);
        _hudView.HideHUD();
        _hudView.IsPauseMenuActive = false;
        _hudView.ShowStageFailPanel();
    }

    // --- Clear Sequence ---
    private void HandleStageClear() => StartCoroutine(StageClearSequence());

    private IEnumerator StageClearSequence()
    {
        _stageManager?.PauseGameSmoothly(1.0f);
        yield return new WaitForSecondsRealtime(1.0f);
        _hudView.HideHUD();
        _hudView.IsPauseMenuActive = false;

        UpdateStageClearStarImage();
        if (_stageManager != null)
        {
            _hudView.UpdateStageClearTimeRecordText(TimeSpan.FromSeconds(_stageManager.StageTimer));
        }
        if (GameSaveManager.Instance != null)
        {
            _hudView.UpdateStageClearStageNumberText(GameSaveManager.Instance.CurrentStageNumber);
        }
        _hudView.ShowStageClearPanel();
    }

    // --- Helper Methods (Object & Stars) ---
    private void UpdateStageObjectText()
    {
        if (_stageManager == null) return;
        int idx = 0;
        foreach (var obj in _stageManager.StageObjects)
        {
            string desc = GetObjectDescription(obj);
            _hudView.UpdateStageObjectUI(idx, desc, obj.isCleared);
            idx++;
        }
    }

    private string GetObjectDescription(StageObject obj)
    {
        if (!MissionTextDict.TryGetValue(obj.stageObjectType, out string format))
        {
            CustomDebug.LogWarning($"알 수 없는 도전과제! : {obj.stageObjectType}");
            return "알 수 없는 도전과제";
        }

        switch (obj.stageObjectType)
        {
            case StageObjectType.TimeLimitClear:
                var ts = TimeSpan.FromSeconds(obj.value);
                return string.Format(format, ts.Minutes, ts.Seconds);

            case StageObjectType.RemainHealthClear:
                return string.Format(format, obj.value);

            case StageObjectType.CollectGemsClear:
                return string.Format(format, obj.value);

            case StageObjectType.NoFallClear:
                return format;

            case StageObjectType.NoDamageClear:
                return format;

            default:
                return format;
        }
    }

    private void UpdateStageClearStarImage()
    {
        if (_stageManager == null) return;
        int idx = 0;
        foreach (var obj in _stageManager.StageObjects)
        {
            if (obj.isCleared) _hudView.UpdateStageClearStarSprite(idx++, true);
        }
        for (int i = idx; i < 3; i++) _hudView.UpdateStageClearStarSprite(i, false);
    }
}