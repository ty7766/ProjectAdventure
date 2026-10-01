#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Android 진동 백엔드. 기기 능력에 따라 단계적으로 내려간다.
/// 1) Android 11+ & 프리미티브 지원: VibrationEffect.Composition (HD 햅틱)
/// 2) Android 8+: 진폭 제어 파형 (진폭 제어 미지원 기기는 가벼운 진동 생략)
/// 3) Android 7.1: 켜짐/꺼짐 패턴
/// 만든 VibrationEffect는 프리셋/세기별로 캐싱해 JNI 생성 비용을 한 번만 낸다.
/// VIBRATE 권한은 Editor/AndroidVibratePermission이 빌드 시 매니페스트에 넣는다.
/// </summary>
public static class AndroidHaptics
{
    //--- Settings ---//
    private const int ApiWaveform = 26;     // Android 8.0 VibrationEffect
    private const int ApiComposition = 30;  // Android 11 Composition
    private const int ApiExtraPrimitives = 31;
    private const int IntensitySteps = 20;  // 캐시 키용 세기 양자화

    //--- Fields ---//
    private static bool _isInitialized;
    private static bool _isAvailable;
    private static int _sdk;
    private static bool _hasAmplitudeControl;
    private static AndroidJavaObject _vibrator;
    private static AndroidJavaClass _effectClass;
    private static readonly Dictionary<int, bool> _primitiveSupport = new Dictionary<int, bool>();
    // 값이 null이면 '이 기기에서는 생략'으로 캐싱
    private static readonly Dictionary<int, AndroidJavaObject> _effectCache = new Dictionary<int, AndroidJavaObject>();

    //--- Properties ---//
    public static bool IsAvailable
    {
        get
        {
            EnsureInitialized();
            return _isAvailable;
        }
    }

    //--- Public Methods ---//
    /// <returns>실제로 진동을 요청했는지</returns>
    public static bool Play(HapticType type, float intensity, HapticPresets.Pattern pattern)
    {
        if (!IsAvailable)
        {
            return false;
        }

        try
        {
            if (_sdk < ApiWaveform)
            {
                return PlayLegacy(pattern);
            }

            int key = (int)type * (IntensitySteps + 1) + Mathf.RoundToInt(Mathf.Clamp01(intensity) * IntensitySteps);
            if (!_effectCache.TryGetValue(key, out AndroidJavaObject effect))
            {
                effect = CreateEffect(pattern);
                _effectCache[key] = effect;
            }
            if (effect == null)
            {
                return false;
            }

            _vibrator.Call("vibrate", effect);
            return true;
        }
        catch (AndroidJavaException e)
        {
            Debug.LogWarning($"[Haptics] {type} 재생 실패, 햅틱을 끕니다: {e.Message}");
            _isAvailable = false;
            return false;
        }
    }

    public static void Cancel()
    {
        if (!IsAvailable)
        {
            return;
        }
        try
        {
            _vibrator.Call("cancel");
        }
        catch (AndroidJavaException)
        {
        }
    }

    //--- Private Methods ---//
    private static void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }
        _isInitialized = true;

        try
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                _sdk = version.GetStatic<int>("SDK_INT");
            }

            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                if (_sdk >= ApiExtraPrimitives)
                {
                    using (AndroidJavaObject manager = activity.Call<AndroidJavaObject>("getSystemService", "vibrator_manager"))
                    {
                        _vibrator = manager?.Call<AndroidJavaObject>("getDefaultVibrator");
                    }
                }
                else
                {
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
            }

            _isAvailable = _vibrator != null && _vibrator.Call<bool>("hasVibrator");
            if (!_isAvailable)
            {
                return;
            }

            if (_sdk >= ApiWaveform)
            {
                _effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                _hasAmplitudeControl = _vibrator.Call<bool>("hasAmplitudeControl");
            }
        }
        catch (AndroidJavaException e)
        {
            Debug.LogWarning($"[Haptics] 진동 초기화 실패: {e.Message}");
            _isAvailable = false;
        }
    }

    private static AndroidJavaObject CreateEffect(HapticPresets.Pattern pattern)
    {
        if (pattern.Primitives != null && CanCompose(pattern.Primitives))
        {
            return CreateComposition(pattern.Primitives);
        }
        if (pattern.Waveform == null || (pattern.IsSubtle && !_hasAmplitudeControl))
        {
            return null;
        }
        return CreateWaveform(pattern.Waveform);
    }

    private static bool CanCompose(HapticPresets.Primitive[] primitives)
    {
        if (_sdk < ApiComposition)
        {
            return false;
        }
        foreach (HapticPresets.Primitive primitive in primitives)
        {
            if (!primitive.IsOptional && !IsPrimitiveSupported(primitive.Id))
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsPrimitiveSupported(int id)
    {
        if (_primitiveSupport.TryGetValue(id, out bool isSupported))
        {
            return isSupported;
        }

        bool needsExtraApi = id == HapticPresets.PrimitiveThud || id == HapticPresets.PrimitiveSpin || id == HapticPresets.PrimitiveLowTick;
        isSupported = _sdk >= ApiComposition
            && (!needsExtraApi || _sdk >= ApiExtraPrimitives)
            && _vibrator.Call<bool>("areAllPrimitivesSupported", new[] { id });
        _primitiveSupport[id] = isSupported;
        return isSupported;
    }

    private static AndroidJavaObject CreateComposition(HapticPresets.Primitive[] primitives)
    {
        using (AndroidJavaObject composition = _effectClass.CallStatic<AndroidJavaObject>("startComposition"))
        {
            int carriedDelay = 0;
            foreach (HapticPresets.Primitive primitive in primitives)
            {
                if (!IsPrimitiveSupported(primitive.Id))
                {
                    // 빠진 선택 프리미티브의 딜레이는 다음 프리미티브로 넘겨 리듬을 유지
                    carriedDelay += primitive.DelayMs;
                    continue;
                }
                // addPrimitive는 같은 빌더를 반환하므로 반환 참조는 바로 해제
                composition.Call<AndroidJavaObject>("addPrimitive", primitive.Id, primitive.Scale, primitive.DelayMs + carriedDelay)?.Dispose();
                carriedDelay = 0;
            }
            return composition.Call<AndroidJavaObject>("compose");
        }
    }

    private static AndroidJavaObject CreateWaveform(HapticPresets.Segment[] waveform)
    {
        var timings = new long[waveform.Length];
        var amplitudes = new int[waveform.Length];
        for (int i = 0; i < waveform.Length; i++)
        {
            timings[i] = waveform[i].DurationMs;
            amplitudes[i] = waveform[i].Amplitude <= 0f ? 0 : Mathf.Clamp(Mathf.RoundToInt(waveform[i].Amplitude * 255f), 1, 255);
        }
        return _effectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1);
    }

    /// <summary>Android 7.1: 진폭 없이 켜짐/꺼짐 패턴으로만 재생 (가벼운 진동은 생략)</summary>
    private static bool PlayLegacy(HapticPresets.Pattern pattern)
    {
        if (pattern.IsSubtle || pattern.Waveform == null)
        {
            return false;
        }

        // vibrate(long[] pattern, int repeat): [대기, 켜짐, 꺼짐, 켜짐, ...]
        var timings = new List<long> { 0 };
        bool isOn = false;
        foreach (HapticPresets.Segment segment in pattern.Waveform)
        {
            // 약한 꼬리 구간은 켜짐으로 치면 진동이 늘어지므로 꺼짐으로 본다
            bool segmentOn = segment.Amplitude >= 0.3f;
            if (segmentOn == isOn)
            {
                timings[timings.Count - 1] += segment.DurationMs;
            }
            else
            {
                timings.Add(segment.DurationMs);
                isOn = segmentOn;
            }
        }
        if (timings.Count < 2)
        {
            return false;
        }

        _vibrator.Call("vibrate", timings.ToArray(), -1);
        return true;
    }
}
#endif
