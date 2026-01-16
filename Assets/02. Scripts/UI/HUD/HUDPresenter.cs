using UnityEngine;

public class HUDPresenter : MonoBehaviour
{
    //--- Settings ---//
    [Header("References")]
    [SerializeField] private PlayerProperties _playerModel;
    [SerializeField] private HUDView _healthView;

    //--- Unity Methods ---//
    private void Start()
    {
        SubscribeEventHandlers();
    }

    private void SubscribeEventHandlers()
    {
        UnsubscribeEventHandlers();
    }

    //--- Private Methods ---//
    private void UnsubscribeEventHandlers()
    {
        if (_playerModel != null)
        {
            _healthView.UpdateHealthUI(_playerModel.Health);
            _playerModel.OnHealthChanged += HandleHealthChanged;
        }
    }

    private void OnDestroy()
    {
        if (_playerModel != null)
        {
            _playerModel.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private void HandleHealthChanged(int newHealth)
    {
        _healthView.UpdateHealthUI(newHealth);
    }
}
