using UnityEngine;

public static class PlatformCapability
{
    public static bool UseTouchUI { get; private set; }

    /// <summary>
    /// 진동(햅틱)을 낼 수 있는 플랫폼인지. 실제 진동 모터 유무는 백엔드가 기기에서 다시 확인한다.
    /// 터치 UI 여부와는 별개 (에디터 터치 시뮬레이션 중에도 진동은 낼 수 없다)
    /// </summary>
    public static bool SupportsHaptics { get; private set; }

#if UNITY_EDITOR
    private const string EditorForceTouchUIKey = "PlatformCapability.EditorForceTouchUI";
    private const string EditorLogHapticsKey = "PlatformCapability.EditorLogHaptics";

    public static bool EditorForceTouchUI
    {
        get => UnityEditor.EditorPrefs.GetBool(EditorForceTouchUIKey, false);
        set => UnityEditor.EditorPrefs.SetBool(EditorForceTouchUIKey, value);
    }

    /// <summary>에디터에서 진동 대신 콘솔에 햅틱 호출을 기록 (타이밍 확인용)</summary>
    public static bool EditorLogHaptics
    {
        get => UnityEditor.EditorPrefs.GetBool(EditorLogHapticsKey, false);
        set => UnityEditor.EditorPrefs.SetBool(EditorLogHapticsKey, value);
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        UseTouchUI = false;
        SupportsHaptics = false;
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

    private static bool EvaluateHaptics()
    {
#if UNITY_EDITOR
        // 에디터: 진동 장치 없음 (EditorLogHaptics로 호출만 확인)
        return false;
#elif UNITY_ANDROID
        return true;
#else
        return false;
#endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Init()
    {
        UseTouchUI = Evaluate();
        SupportsHaptics = EvaluateHaptics();
    }
}
