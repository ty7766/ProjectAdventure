Shader "Custom/PlayerOccluded"
{
    Properties
    {
        _Color       ("Outline Color",  Color)        = (0.2, 0.8, 1.0, 1.0)
        _RimPower    ("Rim Power",      Range(0.1, 16)) = 5.0
        _RimThreshold("Rim Threshold",  Range(0, 1))    = 0.35
        _RimSoftness ("Rim Softness",   Range(0.001, 1))= 0.25
        _Intensity   ("Intensity",      Range(0, 8))    = 2.5
        _Alpha       ("Alpha",          Range(0, 1))    = 0.9
        _FillBias    ("Fill Bias",      Range(0, 1))    = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }

        Pass
        {
            Name "PlayerOccluded"
            Tags { "LightMode" = "UniversalForward" }

            ZTest Greater
            ZWrite Off
            Cull Back
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _RimPower;
                float  _RimThreshold;
                float  _RimSoftness;
                float  _Intensity;
                float  _Alpha;
                float  _FillBias;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs posIn = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmIn = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posIn.positionCS;
                OUT.normalWS    = nrmIn.normalWS;
                OUT.viewDirWS   = GetWorldSpaceViewDir(posIn.positionWS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);

                float rim     = 1.0 - saturate(dot(N, V));
                float rimPow  = pow(rim, _RimPower);
                float lo      = saturate(_RimThreshold);
                float hi      = saturate(lo + _RimSoftness);
                float rimEdge = smoothstep(lo, hi, rimPow);
                float shape   = saturate(lerp(_FillBias, 1.0, rimEdge));

                half3 col = _Color.rgb * shape * _Intensity;
                half  a   = _Alpha * shape;
                return half4(col, a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
