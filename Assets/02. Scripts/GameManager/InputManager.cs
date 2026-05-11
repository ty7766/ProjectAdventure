using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : Singleton<InputManager>
{
    private GameControls _gameControls;

    public GameControls GameControls => _gameControls;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            return;
        }

        _gameControls = new GameControls();
        LoadKeyBindings();
    }

    private void OnDestroy()
    {
        _gameControls?.Dispose();
    }

    private void LoadKeyBindings()
    {
        if (_gameControls == null) return;

        var playerActionMap = _gameControls.asset.FindActionMap("Player");
        var mapActionMap = _gameControls.asset.FindActionMap("Map");

        if (playerActionMap != null)
        {
            var moveAction = playerActionMap.FindAction("Move");
            if (moveAction != null)
            {
                ApplySavedBinding(moveAction, "Move/up", "up");
                ApplySavedBinding(moveAction, "Move/down", "down");
                ApplySavedBinding(moveAction, "Move/left", "left");
                ApplySavedBinding(moveAction, "Move/right", "right");
            }
        }

        if (mapActionMap != null)
        {
            var selectMapAction = mapActionMap.FindAction("SelectMap");
            var changeMapAction = mapActionMap.FindAction("ChangeMap");

            if (selectMapAction != null)
            {
                ApplySavedBinding(selectMapAction, "SelectMap/negative", "negative");
                ApplySavedBinding(selectMapAction, "SelectMap/positive", "positive");
            }

            if (changeMapAction != null)
            {
                ApplySavedBinding(changeMapAction, "ChangeMap/negative", "negative");
                ApplySavedBinding(changeMapAction, "ChangeMap/positive", "positive");
            }
        }
    }

    private void ApplySavedBinding(InputAction action, string actionIdentifier, string bindingName)
    {
        string savedPath = PlayerPrefs.GetString($"KeyBind_{actionIdentifier}", "");

        if (string.IsNullOrEmpty(savedPath))
        {
            return;
        }

        int bindingIndex = -1;
        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (action.bindings[i].name == bindingName)
            {
                bindingIndex = i;
                break;
            }
        }

        if (bindingIndex >= 0)
        {
            action.ApplyBindingOverride(bindingIndex, savedPath);
        }
    }
}
