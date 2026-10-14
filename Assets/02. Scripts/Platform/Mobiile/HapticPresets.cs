using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 햅틱 프리셋 정의 (플랫폼 무관 데이터).
/// - Primitives: Android 11+ 고품질 진동(VibrationEffect.Composition). 기기가 모두 지원할 때만 사용
/// - Waveform: 진폭 제어 파형. Primitives 미지원 기기 및 Android 8~10의 폴백 (구형 기기에서는 켜짐/꺼짐 패턴으로 변환)
/// - IsSubtle: 진폭 제어가 안 되는 기기에서는 생략할 가벼운 진동 (켜짐/꺼짐만으로는 '틱'이 아니라 '부르르'가 되어 오히려 거슬림)
/// intensity(0~1)는 Primitive 세기와 Waveform 진폭에 곱해진다.
/// </summary>
public static class HapticPresets
{
    //--- Android VibrationEffect.Composition.PRIMITIVE_* ---//
    public const int PrimitiveClick = 1;
    public const int PrimitiveThud = 2;      // API 31+
    public const int PrimitiveSpin = 3;      // API 31+
    public const int PrimitiveQuickRise = 4;
    public const int PrimitiveSlowRise = 5;
    public const int PrimitiveQuickFall = 6;
    public const int PrimitiveTick = 7;
    public const int PrimitiveLowTick = 8;   // API 31+

    //--- Nested Types ---//
    public readonly struct Primitive
    {
        public readonly int Id;
        public readonly float Scale;
        public readonly int DelayMs;
        /// <summary>미지원 기기에서는 이 프리미티브만 빼고 재생 (전체를 Waveform으로 내리지 않음)</summary>
        public readonly bool IsOptional;

        public Primitive(int id, float scale, int delayMs = 0, bool isOptional = false)
        {
            Id = id;
            Scale = scale;
            DelayMs = delayMs;
            IsOptional = isOptional;
        }
    }

    public readonly struct Segment
    {
        public readonly int DurationMs;
        /// <summary>0~1 정규화 진폭 (0 = 쉼)</summary>
        public readonly float Amplitude;

        public Segment(int durationMs, float amplitude)
        {
            DurationMs = durationMs;
            Amplitude = amplitude;
        }
    }

    public sealed class Pattern
    {
        public Primitive[] Primitives;
        public Segment[] Waveform;
        public bool IsSubtle;

        /// <summary>대략적인 재생 길이 (Waveform 기준, Primitive도 비슷한 길이로 맞춰 둠)</summary>
        public int DurationMs
        {
            get
            {
                int total = 0;
                if (Waveform != null)
                {
                    foreach (Segment segment in Waveform)
                    {
                        total += segment.DurationMs;
                    }
                }
                return total;
            }
        }
    }

    //--- Public Methods ---//
    public static Pattern Get(HapticType type, float intensity)
    {
        intensity = Mathf.Clamp01(intensity);
        Pattern pattern = Build(type);
        if (pattern == null)
        {
            return null;
        }

        // 세기 반영 (Primitive scale은 0~1, 진폭은 0이 아닌 구간이 너무 약해 사라지지 않도록 하한)
        if (pattern.Primitives != null)
        {
            for (int i = 0; i < pattern.Primitives.Length; i++)
            {
                Primitive p = pattern.Primitives[i];
                pattern.Primitives[i] = new Primitive(p.Id, Mathf.Clamp01(p.Scale * intensity), p.DelayMs, p.IsOptional);
            }
        }
        if (pattern.Waveform != null)
        {
            for (int i = 0; i < pattern.Waveform.Length; i++)
            {
                Segment s = pattern.Waveform[i];
                float amplitude = s.Amplitude <= 0f ? 0f : Mathf.Max(0.04f, s.Amplitude * intensity);
                pattern.Waveform[i] = new Segment(s.DurationMs, amplitude);
            }
        }
        return pattern;
    }

    //--- Private Methods ---//
    private static Pattern Build(HapticType type)
    {
        switch (type)
        {
            case HapticType.Tap:
                return new Pattern
                {
                    Primitives = new[] { new Primitive(PrimitiveTick, 0.7f) },
                    Waveform = new[] { new Segment(12, 0.35f) },
                    IsSubtle = true,
                };

            case HapticType.Selection:
                return new Pattern
                {
                    Primitives = new[] { new Primitive(PrimitiveTick, 0.45f) },
                    Waveform = new[] { new Segment(8, 0.25f) },
                    IsSubtle = true,
                };

            case HapticType.Denied:
                // 두 번 툭툭 - "안 돼"
                return new Pattern
                {
                    Primitives = new[] { new Primitive(PrimitiveTick, 1f), new Primitive(PrimitiveTick, 1f, 70) },
                    Waveform = new[] { new Segment(20, 0.65f), new Segment(60, 0f), new Segment(20, 0.65f) },
                };

            case HapticType.CountdownTick:
                return new Pattern
                {
                    Primitives = new[] { new Primitive(PrimitiveClick, 0.5f) },
                    Waveform = new[] { new Segment(15, 0.45f) },
                    IsSubtle = true,
                };

            case HapticType.CountdownGo:
                return new Pattern
                {
                    Primitives = new[] { new Primitive(PrimitiveThud, 0.8f, 0, true), new Primitive(PrimitiveClick, 1f) },
                    Waveform = new[] { new Segment(30, 0.9f), new Segment(40, 0.35f) },
                };

            case HapticType.GemCollect:
                // 반짝이는 3연타, 점점 약하게 (띵-딩-딩)
                return new Pattern
                {
                    Primitives = new[]
                    {
                        new Primitive(PrimitiveClick, 0.8f),
                        new Primitive(PrimitiveTick, 0.6f, 45),
                        new Primitive(PrimitiveTick, 0.35f, 45),
                    },
                    Waveform = new[]
                    {
                        new Segment(18, 0.8f), new Segment(35, 0f),
                        new Segment(12, 0.5f), new Segment(35, 0f),
                        new Segment(10, 0.3f),
                    },
                };

            case HapticType.MapSwap:
                // 차오르다가 철컥
                return new Pattern
                {
                    Primitives = new[] { new Primitive(PrimitiveQuickRise, 0.4f), new Primitive(PrimitiveClick, 0.9f) },
                    Waveform = new[] { new Segment(15, 0.25f), new Segment(15, 0.5f), new Segment(25, 0.9f) },
                };

            case HapticType.Damage:
                // 묵직한 타격 + 짧게 잦아드는 울림. 거친 질감은 파형이 더 잘 살리므로 Primitive 없이 사용
                return new Pattern
                {
                    Waveform = new[]
                    {
                        new Segment(40, 1f), new Segment(30, 0f),
                        new Segment(70, 0.55f), new Segment(50, 0.25f),
                    },
                };

            case HapticType.Death:
                return new Pattern
                {
                    Waveform = new[]
                    {
                        new Segment(60, 1f), new Segment(40, 0.15f),
                        new Segment(120, 0.7f), new Segment(120, 0.4f), new Segment(160, 0.15f),
                    },
                };

            case HapticType.StarDrop:
                // 별이 가속하며 떨어지는 동안 슬며시 차오름 (박히는 순간 StarImpact가 덮어쓴다)
                return new Pattern
                {
                    Waveform = new[] { new Segment(40, 0.08f), new Segment(40, 0.15f), new Segment(40, 0.25f) },
                    IsSubtle = true,
                };

            case HapticType.StarImpact:
                return BuildStarImpact();

            default:
                return null;
        }
    }

    /// <summary>
    /// 별이 박히는 충격 + 패널 흔들림(0.25초, 감쇠)에 맞춘 여진.
    /// Primitive: 쿵(THUD, 지원 시) + 딱(CLICK) 뒤로 잦아드는 틱.
    /// Waveform: 강한 타격 뒤 진폭이 오르내리며 감쇠 (흔들림의 좌우 진동을 손끝으로).
    /// </summary>
    private static Pattern BuildStarImpact()
    {
        const int impactMs = 35;
        const int rattleSegmentMs = 22;
        const int rattleSegments = 10; // 약 220ms = 흔들림 길이(0.25s) - 타격

        var waveform = new List<Segment> { new Segment(impactMs, 1f) };
        for (int i = 0; i < rattleSegments; i++)
        {
            float decay = Mathf.Lerp(1f, 0.15f, i / (float)(rattleSegments - 1));
            bool isPeak = i % 2 == 0;
            waveform.Add(new Segment(rattleSegmentMs, (isPeak ? 0.55f : 0.12f) * decay));
        }

        return new Pattern
        {
            Primitives = new[]
            {
                new Primitive(PrimitiveThud, 1f, 0, true),
                new Primitive(PrimitiveClick, 1f),
                new Primitive(PrimitiveTick, 0.6f, 40),
                new Primitive(PrimitiveTick, 0.35f, 45),
                new Primitive(PrimitiveTick, 0.15f, 50),
            },
            Waveform = waveform.ToArray(),
        };
    }
}
