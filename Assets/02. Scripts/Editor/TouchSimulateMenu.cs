using UnityEditor;

public static class TouchSimulateMenu
{
    private const string MenuPath = "Tools/Platform Test/Toggle Touch UI Simulation";

    [MenuItem(MenuPath, isValidateFunction: false, priority: 0)]
    private static void Toggle()
    {
        PlatformCapability.EditorForceTouchUI = !PlatformCapability.EditorForceTouchUI;
        UnityEngine.Debug.Log($"Touch simulator = {PlatformCapability.EditorForceTouchUI} (Play 재시작 필요)");
    }

    [MenuItem(MenuPath, isValidateFunction: true, priority: 0)]
    private static bool ToggleValidate()
    {
        UnityEditor.Menu.SetChecked(MenuPath, PlatformCapability.EditorForceTouchUI);
        return true;
    }
}