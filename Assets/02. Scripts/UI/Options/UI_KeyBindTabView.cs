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

    public void Initialize(GameControls gameControls)
    {
        _gameControls = gameControls;
        _presenter = new UI_KeyBindTabPresenter(this, _gameControls);
        _presenter.Initialize();
        BindButtonEvents();
    }

    private void Awake()
    {
    }

    private void Start()
    {
    }

    private void BindButtonEvents()
    {
        foreach (var keyBindButton in _keyBindButtons)
        {
            if (keyBindButton.button != null)
            {
                string actionName = keyBindButton.actionName;
                keyBindButton.button.onClick.AddListener(() => _presenter.OnKeyBindButtonClicked(actionName));
            }
        }
    }

    private void OnDestroy()
    {
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
