using UnityEngine;

public static class PlatformCapability
{
    public static bool UseTouchUI { get; private set; }

#if UNITY_EDITOR
    public static bool EditorForceTouchUI { get; set; }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        UseTouchUI = false;
    }

    private static bool Evaluate()
    {

        
#if UNITY_EDITOR
        if (EditorForceTouchUI) return true;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        return true;
#else
        return false;
#endif

    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Init() => UseTouchUI = Evaluate();
}
