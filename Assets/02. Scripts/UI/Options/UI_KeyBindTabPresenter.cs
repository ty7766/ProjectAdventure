using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class UI_KeyBindTabPresenter
{
    private UI_KeyBindTabView _view;
    private GameControls _gameControls;
    private InputActionAsset _inputActionAsset;
    private string _currentListeningAction;
    private bool _isListening;
    private MonoBehaviour _coroutineRunner;
    private InputActionRebindingExtensions.RebindingOperation _rebindingOperation;
    private Coroutine _timeoutCoroutine;

    private readonly List<string> _bindableActions = new List<string>
    {
        "Move/up",
        "Move/down",
        "Move/left",
        "Move/right",
        "SelectMap/negative",
        "SelectMap/positive",
        "ChangeMap/negative",
        "ChangeMap/positive"
    };

    public UI_KeyBindTabPresenter(UI_KeyBindTabView view, GameControls gameControls)
    {
        _view = view;
        _gameControls = gameControls;
        _inputActionAsset = gameControls.asset;
        _coroutineRunner = view as MonoBehaviour;
    }

    public void Initialize()
    {
        LoadAllKeyBindings();
    }

    private void LoadAllKeyBindings()
    {
        if (_inputActionAsset == null)
        {
            Debug.LogError("InputActionAsset is null!");
            return;
        }

        foreach (var actionIdentifier in _bindableActions)
        {
            string keyText = GetKeyTextForAction(actionIdentifier);
            _view.SetKeyDisplayText(actionIdentifier, keyText);
        }
    }

    private string GetKeyTextForAction(string actionIdentifier)
    {
        string[] parts = actionIdentifier.Split('/');
        string mapName = "Player";
        string actionName = "";
        string bindingName = "";

        if (actionIdentifier.Contains("SelectMap") || actionIdentifier.Contains("ChangeMap"))
        {
            mapName = "Map";
            actionName = parts[0];
            bindingName = parts.Length > 1 ? parts[1] : "";
        }
        else
        {
            actionName = parts[0];
            bindingName = parts.Length > 1 ? parts[1] : "";
        }

        var actionMap = _inputActionAsset.FindActionMap(mapName);
        if (actionMap == null)
        {
            Debug.LogWarning($"ActionMap '{mapName}' not found");
            return "Not Bound";
        }

        var action = actionMap.FindAction(actionName);
        if (action == null)
        {
            Debug.LogWarning($"Action '{actionName}' not found in map '{mapName}'");
            return "Not Bound";
        }

        if (!string.IsNullOrEmpty(bindingName))
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (binding.name == bindingName)
                {
                    string displayName = GetKeyDisplayName(binding);
                    return displayName;
                }
            }
        }
        else if (action.bindings.Count > 0)
        {
            return GetKeyDisplayName(action.bindings[0]);
        }

        return "Not Bound";
    }

    private string GetKeyDisplayName(InputBinding binding)
    {
        string path = binding.overridePath;
        if (string.IsNullOrEmpty(path))
        {
            path = binding.path;
        }
        if (string.IsNullOrEmpty(path))
        {
            return "Not Bound";
        }

        int lastSlash = path.LastIndexOf('/');
        if (lastSlash >= 0)
        {
            return path.Substring(lastSlash + 1).ToUpper();
        }

        return path.ToUpper();
    }

    public void OnKeyBindButtonClicked(string actionIdentifier)
    {
        if (_isListening) return;

        string mapName = "Player";
        string actionName = "";

        if (actionIdentifier.Contains("SelectMap") || actionIdentifier.Contains("ChangeMap"))
        {
            mapName = "Map";
            actionName = actionIdentifier.Split('/')[0];
        }
        else
        {
            actionName = actionIdentifier.Split('/')[0];
        }

        var actionMap = _inputActionAsset.FindActionMap(mapName);
        if (actionMap == null)
        {
            Debug.LogError($"ActionMap '{mapName}' not found");
            return;
        }

        var action = actionMap.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError($"Action '{actionName}' not found in map '{mapName}'");
            return;
        }

        _isListening = true;
        _currentListeningAction = actionIdentifier;
        _view.SetKeyBindButtonInteractable(false);
        _view.ShowListeningState(actionIdentifier);

        action.Disable();

        if (_coroutineRunner != null)
        {
            _coroutineRunner.StartCoroutine(StartRebindingAfterDelay(action, GetBindingIndex(action, actionIdentifier), actionIdentifier));
        }
    }

    private IEnumerator StartRebindingAfterDelay(InputAction action, int bindingIndex, string actionIdentifier)
    {
        yield return null;

        if (!_isListening)
        {
            yield break;
        }

        _rebindingOperation = action.PerformInteractiveRebinding(bindingIndex)
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => OnRebindingComplete(actionIdentifier, action, bindingIndex))
            .OnCancel(operation => EndKeyBinding(updateUI: true))
            .Start();

        if (_coroutineRunner != null)
        {
            _timeoutCoroutine = _coroutineRunner.StartCoroutine(ListeningTimeout());
        }
    }

    private int GetBindingIndex(InputAction action, string actionIdentifier)
    {
        string[] parts = actionIdentifier.Split('/');
        string bindingName = parts.Length > 1 ? parts[1] : "";

        if (!string.IsNullOrEmpty(bindingName))
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].name == bindingName)
                {
                    return i;
                }
            }
        }

        return 0;
    }

    private void OnRebindingComplete(string actionIdentifier, InputAction action, int bindingIndex)
    {
        if (bindingIndex >= 0 && bindingIndex < action.bindings.Count)
        {
            var binding = action.bindings[bindingIndex];
            string keyPath = binding.overridePath;
            if (string.IsNullOrEmpty(keyPath))
            {
                keyPath = binding.path;
            }
            string keyText = GetKeyDisplayName(binding);

            _view.SetKeyDisplayText(actionIdentifier, keyText);
            SaveKeyBinding(actionIdentifier, keyPath);
        }

        action.Enable();
        _rebindingOperation?.Dispose();
        EndKeyBinding(updateUI: false);
    }

    private void SaveKeyBinding(string actionIdentifier, string keyPath)
    {
        PlayerPrefs.SetString($"KeyBind_{actionIdentifier}", keyPath);
        PlayerPrefs.Save();
    }

    public void CancelListening()
    {
        EndKeyBinding(updateUI: false);
    }

    private void EndKeyBinding(bool updateUI = true)
    {
        _isListening = false;

        if (_timeoutCoroutine != null && _coroutineRunner != null)
        {
            _coroutineRunner.StopCoroutine(_timeoutCoroutine);
            _timeoutCoroutine = null;
        }

        if (!string.IsNullOrEmpty(_currentListeningAction))
        {
            if (updateUI)
            {
                string keyText = GetKeyTextForAction(_currentListeningAction);
                _view.SetKeyDisplayText(_currentListeningAction, keyText);
            }

            string mapName = "Player";
            if (_currentListeningAction.Contains("SelectMap") || _currentListeningAction.Contains("ChangeMap"))
            {
                mapName = "Map";
            }

            var actionMap = _inputActionAsset.FindActionMap(mapName);
            var actionName = _currentListeningAction.Split('/')[0];
            var action = actionMap.FindAction(actionName);

            if (action != null)
            {
                action.Enable();
            }
        }

        _currentListeningAction = null;
        _rebindingOperation?.Dispose();
        _view.SetKeyBindButtonInteractable(true);
    }

    private IEnumerator ListeningTimeout()
    {
        yield return new WaitForSeconds(5f);
        if (_isListening)
        {
            EndKeyBinding();
        }
    }
}
