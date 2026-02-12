using System.Collections.Generic;
using UnityEngine;

public class HUDStatusPresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HUDView _hudView;
    [SerializeField] private PlayerProperties _playerModel;
    [SerializeField] private StageManager _stageManager;
    [SerializeField] private PlayerEffectController _effectController;

    private void Awake()
    {
        // View가 없으면 찾아주기 (필요시)
        if (_hudView == null) _hudView = GetComponent<HUDView>();

        SubscribeEventHandlers();
    }

    private void Update()
    {
        HandleTimerUI();
    }

    private void OnDestroy()
    {
        UnsubscribeEventHandlers();
    }

    private void SubscribeEventHandlers()
    {
        if (_playerModel != null)
        {
            _hudView.UpdateHealthUI(_playerModel.Health);
            _playerModel.OnHealthChanged += HandleHealthChanged;
        }
        if (_stageManager != null)
        {
            _stageManager.OnGemCountChanged += HandleGemUpdate;
        }
        if (_effectController != null)
        {
            _effectController.OnBuffListChanged += HandleBuffChange;
        }
    }

    private void UnsubscribeEventHandlers()
    {
        if (_playerModel != null) _playerModel.OnHealthChanged -= HandleHealthChanged;
        if (_stageManager != null) _stageManager.OnGemCountChanged -= HandleGemUpdate;
        if (_effectController != null) _effectController.OnBuffListChanged -= HandleBuffChange;
    }

    private void HandleHealthChanged(int newHealth) => _hudView.UpdateHealthUI(newHealth);
    private void HandleGemUpdate(int current, int total) => _hudView.UpdateGemUI(current, total);
    private void HandleBuffChange(List<ActiveEffect> effects) => _hudView.RefreshBuffs(effects);

    private void HandleTimerUI()
    {
        if (_stageManager != null && _stageManager.IsTimerRunning)
        {
            _hudView.UpdateTimerUI(_stageManager.StageTimer);
        }
    }
}