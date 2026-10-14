using UnityEngine;

/// <summary>
/// 햅틱(진동) 진입점. 호출하는 쪽은 플랫폼을 신경 쓰지 않고 Haptics.Play만 부르면 된다.
/// - PlatformCapability.SupportsHaptics가 아닌 플랫폼(PC/에디터)에서는 아무것도 하지 않는다.
/// - 진동은 새 호출이 이전 진동을 끊으므로, 재생 중인 더 강한 진동은 약한 진동(탭/선택)이 끊지 못하게 한다.
/// - 사용자 설정(Enabled)은 PlayerPrefs에 저장된다.
/// </summary>
public static class Haptics
{
    //--- Settings ---//
    private const string EnabledKey = "HapticsEnabled";

    //--- Fields ---//
    private static bool? _enabled;
    private static int _playingPriority;
    private static float _playingEndTime;

    //--- Properties ---//
    /// <summary>이 기기에서 진동을 낼 수 있는지 (옵션 UI 노출 판단용)</summary>
    public static bool IsSupported
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return PlatformCapability.SupportsHaptics && AndroidHaptics.IsAvailable;
#else
            return false;
#endif
        }
    }

    public static bool Enabled
    {
        get
        {
            _enabled ??= PlayerPrefs.GetInt(EnabledKey, 1) == 1;
            return _enabled.Value;
        }
        set
        {
            _enabled = value;
            PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0);
            if (!value)
            {
                Cancel();
            }
        }
    }

    //--- Unity Methods ---//
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _enabled = null;
        _playingPriority = 0;
        _playingEndTime = 0f;
    }

    //--- Public Methods ---//
    /// <param name="intensity">0~1. 프리셋 세기에 곱해진다</param>
    public static void Play(HapticType type, float intensity = 1f)
    {
        if (type == HapticType.None || !Enabled)
        {
            return;
        }

#if UNITY_EDITOR
        if (PlatformCapability.EditorLogHaptics)
        {
            Debug.Log($"[Haptics] {type} x{intensity:0.00} @ {Time.unscaledTime:0.000}");
        }
#endif
        if (!PlatformCapability.SupportsHaptics)
        {
            return;
        }

        // 언스케일드 시간 기준 (클리어/일시정지 중 timeScale = 0이어도 판정)
        float now = Time.unscaledTime;
        int priority = GetPriority(type);
        if (now < _playingEndTime && priority < _playingPriority)
        {
            return;
        }

        HapticPresets.Pattern pattern = HapticPresets.Get(type, intensity);
        if (pattern == null)
        {
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!AndroidHaptics.Play(type, intensity, pattern))
        {
            return;
        }
#endif
        _playingPriority = priority;
        _playingEndTime = now + pattern.DurationMs / 1000f;
    }

    public static void Cancel()
    {
        _playingPriority = 0;
        _playingEndTime = 0f;
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidHaptics.Cancel();
#endif
    }

    //--- Private Methods ---//
    private static int GetPriority(HapticType type)
    {
        switch (type)
        {
            case HapticType.Tap:
            case HapticType.Selection:
            case HapticType.CountdownTick:
            case HapticType.StarDrop:
                return 0;
            case HapticType.Damage:
            case HapticType.Death:
            case HapticType.StarImpact:
                return 2;
            default:
                return 1;
        }
    }
}
