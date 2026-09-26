Shader "Custom/MapSelectionBeam"
{
    Properties
    {
        [HDR] _BaseColor ("Beam Color (RGB)", Color)      = (0.3, 1.0, 0.8, 1.0)
        _BlockedColor    ("Blocked Color (RGB)", Color)   = (1.0, 0.12, 0.1, 1.0)
        _Blocked         ("Blocked", Range(0, 1))         = 0.0

        [Header(Wall Beams)]
        _BaseGlow        ("Base Glow", Range(0, 4))       = 1.6
        _FalloffPower    ("Height Falloff Power", Range(0.5, 6)) = 2.0
        _CornerBoost     ("Corner Boost", Range(0, 2))    = 0.6
        _Height          ("Beam Height (World Units)", Float) = 2.5

        [Header(Floor Highlight)]
        _FloorEdgeWidth  ("Floor Edge Width", Range(0.002, 0.3)) = 0.06
        _FloorFill       ("Floor Fill Strength", Range(0, 1))    = 0.18

        [Header(Global)]
        _Intensity       ("Intensity", Range(0, 8))       = 2.5
        _PulseSpeed      ("Pulse Speed", Range(0, 10))    = 2.2
        _ScanSpeed       ("Scan Speed", Range(0, 4))      = 0.6
        _ScanDensity     ("Scan Density (per unit)", Range(0.1, 4)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType"     = "Plane"
        }

        Pass
        {
            Name "MapSelectionBeam"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite Off
            Cull Off
            Blend SrcAlpha One

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _BlockedColor;
                half  _Blocked;
                half  _BaseGlow;
                half  _FalloffPower;
                half  _CornerBoost;
                half  _Height;
                half  _FloorEdgeWidth;
                half  _FloorFill;
                half  _Intensity;
                half  _PulseSpeed;
                half  _ScanSpeed;
                half  _ScanDensity;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs posIn = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmIn = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posIn.positionCS;
                OUT.uv          = IN.uv;
                OUT.normalWS    = nrmIn.normalWS;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float t = _TimeParameters.x;   // seconds

                // --- Shared pulse & blocked color blend ------------------------------
                half pulse = 0.85 + 0.15 * sin(t * _PulseSpeed);
                half3 col  = lerp(_BaseColor.rgb, _BlockedColor.rgb, saturate(_Blocked));

                half shape;

                if (abs(normalize(IN.normalWS).y) > 0.5)
                {
                    // =====================================================
                    // Floor quad: glowing border ring + soft pulsing fill
                    // (readable from a top-down / high-angle camera)
                    // =====================================================
                    float2 uv = saturate(IN.uv);
                    float2 toEdge = min(uv, 1.0 - uv);
                    float  edge   = min(toEdge.x, toEdge.y);

                    half band = 1.0 - smoothstep(0.0, _FloorEdgeWidth, edge);
                    band *= 0.9 + 0.1 * sin(t * _PulseSpeed * 1.3 + 2.0);

                    half fill = _FloorFill * pulse;

                    shape = saturate(band * 1.2 + fill);
                }
                else
                {
                    // =====================================================
                    // Wall beams: hottest at the tile border, fading upward
                    // =====================================================
                    float h = saturate(IN.uv.y);   // 0 bottom (tile border) .. 1 top

                    half rise = pow(1.0 - h, _FalloffPower);

                    // hot line where the beam meets the tile edge
                    half baseLine = smoothstep(0.12, 0.0, h) * _BaseGlow;

                    // rising scan keeps the walls alive
                    float scanCoord = h * _Height * _ScanDensity - t * _ScanSpeed;
                    half scan = 0.7 + 0.3 * sin(scanCoord * 6.28318);

                    // slightly brighter near tile corners
                    float wx = abs(IN.uv.x * 2.0 - 1.0);
                    half corner = 1.0 + _CornerBoost * wx * wx;

                    shape = saturate((rise * scan + baseLine) * pulse * corner);
                }

                half3 rgb = col * shape * _Intensity;
                half  a   = shape;

                return half4(rgb, a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
