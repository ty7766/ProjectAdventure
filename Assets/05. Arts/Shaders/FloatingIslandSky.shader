Shader "Custom/FloatingIslandSky"
{
    // 공중섬 연출용 스카이박스.
    // - 위쪽 반구: 파노라마(Equirect) 하늘 텍스처 + 그라디언트 틴트
    // - 아래쪽 반구: 멀리 깔린 "구름 바다"를 절차적 노이즈로 표현 (카메라가 내려다보는 시점 대응)
    Properties
    {
        [NoScaleOffset] _MainTex ("Sky Panorama (Equirect)", 2D) = "white" {}
        _Rotation        ("Panorama Rotation", Range(0, 360)) = 0
        _PanoramaTint    ("Panorama Tint", Color) = (1, 1, 1, 1)
        _PanoramaExposure("Panorama Exposure", Range(0, 4)) = 1.1

        [Header(Gradient)]
        _ZenithColor     ("Zenith Color", Color)  = (0.36, 0.62, 0.95, 1)
        _HorizonColor    ("Horizon Color", Color) = (0.86, 0.93, 1.0, 1)
        _PanoramaBlend   ("Panorama Blend", Range(0, 1)) = 0.75
        _HorizonSharpness("Horizon Sharpness", Range(0.5, 16)) = 4

        [Header(Cloud Sea)]
        _CloudSeaTop     ("Cloud Lit Color", Color)    = (1.0, 1.0, 1.0, 1)
        _CloudSeaShade   ("Cloud Shade Color", Color)  = (0.72, 0.80, 0.93, 1)
        _CloudSeaGap     ("Gap (Deep Sky) Color", Color) = (0.48, 0.66, 0.92, 1)
        _CloudScale      ("Cloud Scale", Float) = 1.6
        _CloudCoverage   ("Cloud Coverage", Range(0, 1)) = 0.55
        _CloudSoftness   ("Cloud Softness", Range(0.01, 0.5)) = 0.06
        _CloudSpeed      ("Cloud Drift (XY)", Vector) = (0.006, 0.003, 0, 0)
        _HorizonHaze     ("Horizon Haze Height", Range(0.01, 0.6)) = 0.18
    }

    SubShader
    {
        Tags
        {
            "Queue"          = "Background"
            "RenderType"     = "Background"
            "PreviewType"    = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 dirOS       : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float  _Rotation;
                float4 _PanoramaTint;
                float  _PanoramaExposure;
                float4 _ZenithColor;
                float4 _HorizonColor;
                float  _PanoramaBlend;
                float  _HorizonSharpness;
                float4 _CloudSeaTop;
                float4 _CloudSeaShade;
                float4 _CloudSeaGap;
                float  _CloudScale;
                float  _CloudCoverage;
                float  _CloudSoftness;
                float4 _CloudSpeed;
                float  _HorizonHaze;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.dirOS       = IN.positionOS.xyz;
                return OUT;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // 모바일 고려: 3 옥타브
            float Fbm(float2 p)
            {
                float v = 0.0;
                v += 0.5    * ValueNoise(p); p = p * 2.03 + 17.1;
                v += 0.25   * ValueNoise(p); p = p * 2.01 + 31.7;
                v += 0.125  * ValueNoise(p);
                return v / 0.875;
            }

            float2 DirToEquirectUV(float3 dir)
            {
                float rad = radians(_Rotation);
                float s = sin(rad), c = cos(rad);
                dir.xz = float2(dir.x * c - dir.z * s, dir.x * s + dir.z * c);
                float u = atan2(dir.x, dir.z) / (2.0 * PI) + 0.5;
                float v = asin(clamp(dir.y, -1.0, 1.0)) / PI + 0.5;
                return float2(u, v);
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 dir = normalize(IN.dirOS);
                float  y   = dir.y;

                // --- 위쪽 하늘 ---
                float  upT      = saturate(y);
                float3 gradient = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(upT, 1.0 / _HorizonSharpness * 2.0));
                float2 uv       = DirToEquirectUV(dir);
                uv.y            = max(uv.y, 0.5); // 아래쪽은 파노라마의 반사 영역이므로 사용하지 않음
                float3 pano     = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb * _PanoramaTint.rgb * _PanoramaExposure;
                float3 sky      = lerp(gradient, pano, _PanoramaBlend * smoothstep(0.04, 0.25, y));

                // --- 아래쪽 구름 바다 (무한 평면 투영) ---
                float  downT    = saturate(-y);
                float2 planeUV  = dir.xz / max(downT, 0.04) * _CloudScale;
                planeUV        += _Time.y * _CloudSpeed.xy * 10.0;
                float  n        = Fbm(planeUV);
                float  n2       = Fbm(planeUV * 0.35 + 7.3);
                float  cloud    = smoothstep(1.0 - _CloudCoverage - _CloudSoftness, 1.0 - _CloudCoverage + _CloudSoftness, n * 0.65 + n2 * 0.35);
                float  shade    = saturate((n - 0.35) * 1.6);
                float3 cloudCol = lerp(_CloudSeaShade.rgb, _CloudSeaTop.rgb, shade);
                float3 sea      = lerp(_CloudSeaGap.rgb, cloudCol, cloud);

                // 지평선 가까이는 연무로 섞어 경계를 부드럽게
                float  haze     = 1.0 - smoothstep(0.0, _HorizonHaze, downT);
                haze           *= haze;
                haze            = max(haze, 1.0 - smoothstep(0.0, 0.03, downT)); // 수평 근처 앨리어싱 완전 차단
                sea             = lerp(sea, _HorizonColor.rgb, haze);

                float3 col = y >= 0.0 ? sky : sea;
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
