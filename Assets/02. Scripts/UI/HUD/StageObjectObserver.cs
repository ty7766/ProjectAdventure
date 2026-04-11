using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class StageObjectObserver : MonoBehaviour
{
    [Header("Components")]
    [SerializeField]
    private StageManager _stageManager;

    [Header("UI Settings")]
    [SerializeField]
    private Sprite _filledStar;
    [SerializeField]
    private Sprite _unfilledStar;
    [SerializeField]
    private string _successString = "\n<color=#1faf54><size=130%>성공";
    [SerializeField]
    private string _failedString = "\n<color=#af1a2f><size=130%>실패";
    [SerializeField]
    private Color _successColor = new Color32(0xFF,0xF4, 0xA0, 188);
    [SerializeField]
    private Color _failedColor = new Color32(0xCB, 0x66, 0x65, 188);

    private List<int> _timeLimits = new List<int>();

    public void Setup(StageManager stageManager)
    {
        UnsubscribeAllEvents();
        _stageManager = stageManager;
        _timeLimits.Clear();

        if (!_stageManager || !_stageManager.PlayerController)
        {
            CustomDebug.LogError("[Stage Object Observer] Stage Manager or Player Controller is missing!", this);
            return;
        }

        var uniqueTypes = _stageManager.StageObjects.Select(obj => obj.stageObjectType).Distinct();

        foreach (var type in uniqueTypes)
        {
            switch (type)
            {
                case StageObjectType.CollectGemsClear:
                    //보석 수집 이벤트 구독
                    _stageManager.OnGemCountChanged += HandleCollectedGems;
                    break;

                case StageObjectType.NoFallClear:
                    //추락 이벤트 구독
                    _stageManager.PlayerController.OnPlayerFallenDown += HandlePlayerFallenDown;
                    break;

                case StageObjectType.RemainHealthClear:
                    //데미지 이벤트 구독
                    //TODO : 회복 기능이 추가되는 경우에는 체력 변경 이벤트를 별도로 만들어 구독하고, 회복 시 도전과제 달성을 추적해야 함.
                    _stageManager.PlayerController.OnPlayerDamageTaken += HandlePlayerHealthChanged;
                    break;

                case StageObjectType.NoDamageClear:
                    //데미지 이벤트 구독
                    _stageManager.PlayerController.OnPlayerDamageTaken += HandlePlayerTakenDamage;
                    break;

                case StageObjectType.TimeLimitClear:
                    break;

                default:
                    CustomDebug.LogError($"[StageObjectObserver] Not implemented object type!! : {type}");
                    break;
            }
        }

        //시간 제한 도전과제 관리
        var timeLimitObjects = _stageManager.StageObjects.Where(obj => obj.stageObjectType == StageObjectType.TimeLimitClear);
        foreach(var timeLimitObject in timeLimitObjects)
        {
            _timeLimits.Add(timeLimitObject.value);
        }
    }

    private void Update()
    {
        if (_timeLimits.Count <= 0 || !_stageManager)
        {
            return;
        }

        for (int i = _timeLimits.Count - 1; i >= 0; i--)
        {
            if (_stageManager.StageTimer > _timeLimits[i])
            {
                var ts = TimeSpan.FromSeconds(_timeLimits[i]);
                if(!HUDFlowPresenter.MissionTextDict.TryGetValue(StageObjectType.TimeLimitClear, out string format))
                {
                    CustomDebug.LogError("[Stage Object Observer] MissionTextDict : No 'TimeLimitClear' Key !!!", this);
                    _timeLimits.RemoveAt(i);
                    continue;
                }
                string content = string.Format(format, ts.Minutes, ts.Seconds);
                content += _failedString;
                NotificationPresenter.AddPopup(new PopupContext(content, _unfilledStar, _failedColor));

                _timeLimits.RemoveAt(i);
            }
        }
    }

    private void OnDestroy()
    {
        UnsubscribeAllEvents();
    }

    private void HandleCollectedGems(int collectedNumberOfGems, int totalNumberOfGems)
    {
        var gemObjects = _stageManager.StageObjects
                .Where(obj => obj.stageObjectType == StageObjectType.CollectGemsClear);

        foreach (var target in gemObjects)
        {
            if(collectedNumberOfGems >= target.value && !target.isCleared)
            {
                target.isCleared = true;
                if(!HUDFlowPresenter.MissionTextDict.TryGetValue(StageObjectType.CollectGemsClear, out var format))
                {
                    CustomDebug.LogError("[Stage Object Observer] MissionTextDict : No 'CollectGemsClear' Key !!!", this);
                    return;
                }
                string content = string.Format(format, target.value);
                content += _successString;
                NotificationPresenter.AddPopup(new PopupContext(content, _filledStar, _successColor));
            }
        }

        if (gemObjects.All(obj => obj.isCleared))
        {
            _stageManager.OnGemCountChanged -= HandleCollectedGems;
        }
    }

    private void HandlePlayerFallenDown()
    {
        //한 번만 알림을 표시하고 이벤트 구독 해제
        _stageManager.PlayerController.OnPlayerFallenDown -= HandlePlayerFallenDown;
        if(!HUDFlowPresenter.MissionTextDict.TryGetValue(StageObjectType.NoFallClear, out var format))
        {
            CustomDebug.LogError("[Stage Object Observer] MissionTextDict : No 'NoFallClear' Key !!!", this);
            return;
        }
        string content = format;
        content += _failedString;
        NotificationPresenter.AddPopup(new PopupContext(content, _unfilledStar, _failedColor));
    }

    private void HandlePlayerTakenDamage()
    {
        //한 번만 알림을 표시하고 이벤트 구독 해제
        _stageManager.PlayerController.OnPlayerDamageTaken -= HandlePlayerTakenDamage;
        if(!HUDFlowPresenter.MissionTextDict.TryGetValue(StageObjectType.NoDamageClear, out var format))
        {
            CustomDebug.LogError("[Stage Object Observer] MissionTextDict : No 'NoDamageClear' key !!!", this);
            return;
        }

        string content = format;
        content += _failedString;
        NotificationPresenter.AddPopup(new PopupContext(content, _unfilledStar, _failedColor));
    }

    private void HandlePlayerHealthChanged()
    {
        var healthObjects = _stageManager.StageObjects
            .Where(obj => obj.stageObjectType == StageObjectType.RemainHealthClear);

        foreach (var healthObject in healthObjects)
        {
            if(_stageManager.PlayerController.Health < healthObject.value && healthObject.isCleared)
            {
                healthObject.isCleared = false;
                if(!HUDFlowPresenter.MissionTextDict.TryGetValue(StageObjectType.RemainHealthClear, out var format))
                {
                    CustomDebug.LogError("[Stage Object Observer] MissionTextDict : No 'RemainHealthClear' key !!!", this);
                    return;
                }
                string content = string.Format(format, healthObject.value);
                content += _failedString;
                NotificationPresenter.AddPopup(new PopupContext(content, _unfilledStar, _failedColor));
            }
        }

        if(healthObjects.All(obj => obj.isCleared == false))
        {
            _stageManager.PlayerController.OnPlayerDamageTaken -= HandlePlayerHealthChanged;
        }
    }

    private void UnsubscribeAllEvents()
    {
        if (!_stageManager)
        {
            return;
        }
        _stageManager.OnGemCountChanged -= HandleCollectedGems;

        if(_stageManager.PlayerController)
        {
            _stageManager.PlayerController.OnPlayerFallenDown -= HandlePlayerFallenDown;
            _stageManager.PlayerController.OnPlayerDamageTaken -= HandlePlayerTakenDamage;
            _stageManager.PlayerController.OnPlayerDamageTaken -= HandlePlayerHealthChanged;
        }
    }
}