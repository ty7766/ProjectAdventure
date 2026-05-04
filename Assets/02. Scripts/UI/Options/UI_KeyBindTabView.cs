using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_KeyBindTabView : MonoBehaviour
{
    [System.Serializable]
    public class KeyBindButton
    {
        public Button button;
        public TextMeshProUGUI keyDisplayText;
        public string actionName;
    }

    [SerializeField] private KeyBindButton[] _keyBindButtons;
    private UI_KeyBindTabPresenter _presenter;
    private GameControls _gameControls;
    private bool _isInitialized = false;

    private void Start()
    {
        TryInitialize();
    }

    private bool TryInitialize()
    {
        if (_isInitialized) return true;

        var inputManager = InputManager.Instance;
        GameControls controls = inputManager?.GameControls;

        if (inputManager != null && controls != null)
        {
            Initialize(controls);
            _isInitialized = true;
            return true;
        }

        Debug.LogWarning("[UI_KeyBindTabView] InputManager.GameControls is not ready yet. Initialization deferred.");
        return false;
    }

    public void Initialize(GameControls gameControls)
    {
        _gameControls = gameControls;
        _presenter = new UI_KeyBindTabPresenter(this, _gameControls);
        _presenter.Initialize();
        BindButtonEvents();
    }

    public void RefreshKeyBindData()
    {
        // 아직 초기화되지 않았으면 시도
        if (!_isInitialized)
        {
            TryInitialize();
        }

        if (_presenter != null)
        {
            _presenter.Initialize();
        }
    }

    private void BindButtonEvents()
    {
        if (_presenter == null)
        {
            Debug.LogWarning("[UI_KeyBindTabView] Presenter is not initialized yet. Button events binding deferred.");
            return;
        }

        foreach (var keyBindButton in _keyBindButtons)
        {
            if (keyBindButton.button != null)
            {
                string actionName = keyBindButton.actionName;
                keyBindButton.button.onClick.AddListener(() => _presenter.OnKeyBindButtonClicked(actionName));
            }
        }
    }

    private void OnDisable()
    {
        _presenter?.CancelListening();
    }

    private void OnDestroy()
    {
        _presenter?.CancelListening();

        foreach (var keyBindButton in _keyBindButtons)
        {
            if (keyBindButton.button != null)
            {
                keyBindButton.button.onClick.RemoveAllListeners();
            }
        }
    }

    public void SetKeyDisplayText(string actionName, string keyText)
    {
        foreach (var keyBindButton in _keyBindButtons)
        {
            if (keyBindButton.actionName == actionName && keyBindButton.keyDisplayText != null)
            {
                keyBindButton.keyDisplayText.text = keyText;
                break;
            }
        }
    }

    public void SetKeyBindButtonInteractable(bool interactable)
    {
        foreach (var keyBindButton in _keyBindButtons)
        {
            if (keyBindButton.button != null)
            {
                keyBindButton.button.interactable = interactable;
            }
        }
    }

    public void ShowListeningState(string actionName)
    {
        SetKeyDisplayText(actionName, "Press any key...");
    }

    public TextMeshProUGUI GetKeyDisplayText(string actionName)
    {
        foreach (var keyBindButton in _keyBindButtons)
        {
            if (keyBindButton.actionName == actionName)
            {
                return keyBindButton.keyDisplayText;
            }
        }
        return null;
    }
}
