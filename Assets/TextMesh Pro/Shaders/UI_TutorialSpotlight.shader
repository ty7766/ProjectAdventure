Shader "UI/TutorialSpotlight"
  {
      Properties
      {
          _Color ("Overlay Color", Color) = (0, 0, 0, 0.8)
          _HoleCenter ("Hole Center (Screen Pixels)", Vector) = (960, 540, 0, 0)
          _HoleSize ("Hole Size (Pixels)", Vector) = (200, 200, 0, 0)
          _CornerRadius ("Corner Radius", Float) = 20
          _Softness ("Edge Softness", Float) = 4
      }

      SubShader
      {
          Tags
          {
              "Queue" = "Overlay"
              "IgnoreProjector" = "True"
              "RenderType" = "Transparent"
          }

          Blend SrcAlpha OneMinusSrcAlpha
          Cull Off
          ZWrite Off
          ZTest Always

          Pass
          {
              CGPROGRAM
              #pragma vertex vert
              #pragma fragment frag
              #include "UnityCG.cginc"

              struct appdata
              {
                  float4 vertex : POSITION;
              };

              struct v2f
              {
                  float4 vertex : SV_POSITION;
                  float2 screenPos : TEXCOORD0;
              };

              float4 _Color;
              float4 _HoleCenter;
              float4 _HoleSize;
              float _CornerRadius;
              float _Softness;

              // Rounded Rectangle SDF
              // p: 중심으로부터의 상대 좌표, b: 반크기(halfSize), r: 코너 반지름
              float RoundedRectSDF(float2 p, float2 b, float r)
              {
                  float2 q = abs(p) - b + r;
                  return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
              }

              v2f vert(appdata v)
              {
                  v2f o;
                  o.vertex = UnityObjectToClipPos(v.vertex);
                  float4 sp = ComputeScreenPos(o.vertex);
                  o.screenPos = sp.xy / sp.w * _ScreenParams.xy;
                  return o;
              }

              fixed4 frag(v2f i) : SV_Target
              {
                  float2 p = i.screenPos - _HoleCenter.xy;
                  float2 halfSize = _HoleSize.xy * 0.5;
                  float dist = RoundedRectSDF(p, halfSize, _CornerRadius);

                  // dist < 0 : 구멍 안쪽 (투명), dist > 0 : 바깥쪽 (불투명)
                  float alpha = smoothstep(-_Softness, _Softness, dist);
                  return fixed4(_Color.rgb, _Color.a * alpha);
              }
              ENDCG
          }
      }
  }