using System;
using System.Collections;
using UnityEngine;

public class HUDFlowPresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HUDView _hudView;
    [SerializeField] private StageManager _stageManager;
    [SerializeField] private PlayerProperties _playerModel;

    private void Awake()
    {
        if (_hudView == null) _hudView = GetComponent<HUDView>();

        if (_stageManager != null) _stageManager.OnStageCleared += HandleStageClear;
        if (_playerModel != null) _playerModel.OnPlayerDeath += HandlePlayerDeath;
    }

    private void Start()
    {
        HandleStageStart();
    }

    private void OnDestroy()
    {
        if (_stageManager != null) _stageManager.OnStageCleared -= HandleStageClear;
        if (_playerModel != null) _playerModel.OnPlayerDeath -= HandlePlayerDeath;
    }

    // --- Start Sequence ---
    private void HandleStageStart()
    {
        _hudView.HideStageFailPanel();
        _hudView.HideHUD();
        _hudView.HidePauseMenu();
        _hudView.HideStageClearPanel();

        UpdateStageObjectText();
        _hudView.ShowStageStartPanel();
        _hudView.ShowStageObjectView();

        StartCoroutine(StartCountDown());
    }

    private IEnumerator StartCountDown()
    {
        _hudView.UpdateStageCountDownContent("준비하세요!");
        _hudView.ApplyStageCountDownAnimation(80f, 1.0f);
        yield return new WaitForSecondsRealtime(1.5f);

        for (int i = 3; i >= 1; i--)
        {
            _hudView.ApplyStageCountDownAnimation(128f, 0.5f);
            _hudView.UpdateStageCountDownContent(i.ToString());
            yield return new WaitForSecondsRealtime(0.5f);
            _hudView.ApplyStageCountDownAnimation(100f, 0.5f);
            yield return new WaitForSecondsRealtime(0.5f);
        }

        _hudView.UpdateStageCountDownContent("GO!");
        _hudView.ApplyStageCountDownAnimation(120f, 0.2f);
        yield return new WaitForSecondsRealtime(0.5f);

        _hudView.HideStageStartPanel();
        _hudView.HideStageObjectView();
        _hudView.ShowHUD();
        _hudView.IsPauseMenuActive = true;

        _stageManager?.StartStage();
    }

    // --- Death Sequence ---
    private void HandlePlayerDeath() => StartCoroutine(PlayerDeathSequence());

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
        switch (obj.stageObjectType)
        {
            case StageObjectType.TimeLimitClear:
                var ts = TimeSpan.FromSeconds(obj.value);
                return $"{ts.Minutes}분 {ts.Seconds}초 이내에 클리어";
            case StageObjectType.NoFallClear: return "한 번도 세상 밖으로 떨어지지 않고 클리어";
            case StageObjectType.NoDamageClear: return "한 번도 데미지를 입지 않고 클리어";
            case StageObjectType.RemainHealthClear: return $"체력을 {obj.value} 이상 남기고 클리어";
            default: return "알 수 없는 도전과제";
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