using UnityEngine;

public static class PlatformCapability
{
    public static bool UseTouchUI { get; private set; }

#if UNITY_EDITOR
    private const string EditorForceTouchUIKey = "PlatformCapability.EditorForceTouchUI";

    public static bool EditorForceTouchUI
    {
        get => UnityEditor.EditorPrefs.GetBool(EditorForceTouchUIKey, false);
        set => UnityEditor.EditorPrefs.SetBool(EditorForceTouchUIKey, value);
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        UseTouchUI = false;
    }

    private static bool Evaluate()
    {
#if UNITY_EDITOR
        // 에디터: 강제 터치 UI 시뮬레이션 설정을 따름
        return EditorForceTouchUI;
#elif UNITY_ANDROID
        // 실기기: Android는 항상 터치 UI 사용
        return true;
#else
        return false;
#endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Init() => UseTouchUI = Evaluate();
}
