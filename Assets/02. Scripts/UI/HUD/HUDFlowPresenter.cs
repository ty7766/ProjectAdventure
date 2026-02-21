using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

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

    [Header("Text Strings")]
    [SerializeField]
    private List<StageMissionData> _missionDataList;
    [SerializeField]
    private string _stageReadyString = "준비하세요!";
    [SerializeField]
    private string _stageGoString = "GO!";

    public static Dictionary<StageObjectType, string> MissionTextDict;
    


    private void Awake()
    {
        MissionTextDict = new Dictionary<StageObjectType, string>();
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
        _hudView.IsPauseMenuActive = false;

        UpdateStageObjectText();
        _hudView.ShowStageStartPanel();
        _hudView.ShowStageObjectView();

        StartCoroutine(StartCountDown());
    }

    private IEnumerator StartCountDown()
    {
        _hudView.UpdateStageCountDownContent(_stageReadyString);
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

        _hudView.UpdateStageCountDownContent(_stageGoString);
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