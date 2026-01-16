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
        if (_playerModel != null)
        {
            _playerModel.OnHealthChanged -= HandleHealthChanged;
        }
        if (_stageManager != null)
        {
            _stageManager.OnGemCountChanged -= HandleGemUpdate;
        }
        if (_effectController != null)
        {
            _effectController.OnBuffListChanged -= HandleBuffChange;
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
}
