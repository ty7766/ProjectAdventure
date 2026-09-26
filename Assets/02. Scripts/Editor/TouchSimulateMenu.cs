using UnityEditor;

public static class TouchSimulateMenu
{
    [MenuItem("Tools/Platform Test/Toggle Touch UI Simulation")]
    private static void Toggle()
    {
        PlatformCapability.EditorForceTouchUI = !PlatformCapability.EditorForceTouchUI;
        UnityEngine.Debug.Log($"Touch simulator = {PlatformCapability.EditorForceTouchUI} (Play 재시작 필요)");
    }
}