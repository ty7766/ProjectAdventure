using UnityEditor;

public static class TouchSimulateMenu
{
    private const string MenuPath = "Tools/Platform Test/Toggle Touch UI Simulation";
    private const string HapticsLogMenuPath = "Tools/Platform Test/Log Haptics";

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

    [MenuItem(HapticsLogMenuPath, isValidateFunction: false, priority: 1)]
    private static void ToggleHapticsLog()
    {
        PlatformCapability.EditorLogHaptics = !PlatformCapability.EditorLogHaptics;
        UnityEngine.Debug.Log($"Haptics log = {PlatformCapability.EditorLogHaptics}");
    }

    [MenuItem(HapticsLogMenuPath, isValidateFunction: true, priority: 1)]
    private static bool ToggleHapticsLogValidate()
    {
        UnityEditor.Menu.SetChecked(HapticsLogMenuPath, PlatformCapability.EditorLogHaptics);
        return true;
    }
}
