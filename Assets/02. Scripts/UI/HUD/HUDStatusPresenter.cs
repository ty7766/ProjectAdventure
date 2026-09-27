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
        if (_playerModel == null) _playerModel = GetComponentInParent<PlayerProperties>();
        if (_effectController == null) _effectController = GetComponentInParent<PlayerEffectController>();
        _playerModel = _playerModel != null ? _playerModel : UnityEngine.Object.FindFirstObjectByType<PlayerProperties>();
        _effectController = _effectController != null ? _effectController : UnityEngine.Object.FindFirstObjectByType<PlayerEffectController>();

        SubscribeEventHandlers();
    }

    /// <summary>
    /// 스테이지 전환 시 StageManager 레퍼런스를 재연결하고 이벤트를 재구독합니다.
    /// 프리팹에서 _playerModel/_effectController가 비어있어도 동작하도록 런타임 주입 경로를 제공합니다.
    /// </summary>
    public void Setup(StageManager stageManager)
    {
        Setup(stageManager, _playerModel, _effectController);
    }

    /// <summary>
    /// 스테이지 전환 시 StageManager/PlayerProperties/PlayerEffectController 레퍼런스를 재연결하고 이벤트를 재구독합니다.
    /// </summary>
    public void Setup(StageManager stageManager, PlayerProperties playerModel, PlayerEffectController effectController)
    {
        // StageManager 재구독 (보석 UI)
        if (_stageManager != null)
        {
            _stageManager.OnGemCountChanged -= HandleGemUpdate;
        }
        _stageManager = stageManager;
        if (_stageManager != null)
        {
            _stageManager.OnGemCountChanged += HandleGemUpdate;
        }

        // PlayerProperties 재구독 (체력 UI) - 중복 구독 방지
        if (!ReferenceEquals(_playerModel, playerModel))
        {
            if (_playerModel != null)
            {
                _playerModel.OnHealthChanged -= HandleHealthChanged;
            }
            _playerModel = playerModel;
            if (_playerModel != null)
            {
                _playerModel.OnHealthChanged += HandleHealthChanged;
            }
        }

        // PlayerEffectController 재구독 (버프 UI) - 중복 구독 방지
        if (!ReferenceEquals(_effectController, effectController))
        {
            if (_effectController != null)
            {
                _effectController.OnBuffListChanged -= HandleBuffChange;
            }
            _effectController = effectController;
            if (_effectController != null)
            {
                _effectController.OnBuffListChanged += HandleBuffChange;
            }
        }

        // 현재 값으로 UI 초기 동기화
        if (_playerModel != null)
        {
            _hudView.UpdateHealthUI(_playerModel.Health);
        }
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