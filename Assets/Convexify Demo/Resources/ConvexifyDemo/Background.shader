// Screen-space backdrop for the demo: a soft vertical gradient with a vignette. Used as the skybox.
Shader "Convexify Demo/Background" {
    Properties {
        _Top ("Top", Color) = (0.105, 0.118, 0.145, 1)
        _Bottom ("Bottom", Color) = (0.043, 0.047, 0.059, 1)
        _Glow ("Glow", Color) = (0.16, 0.19, 0.26, 1)
    }
    SubShader {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Top;
            fixed4 _Bottom;
            fixed4 _Glow;

            struct v2f {
                float4 pos : SV_POSITION;
                float4 screen : TEXCOORD0;
            };

            v2f vert(float4 vertex : POSITION) {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.screen = ComputeScreenPos(o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                float2 uv = i.screen.xy / i.screen.w;
                float3 col = lerp(_Bottom.rgb, _Top.rgb, smoothstep(0, 1, uv.y));
                float2 d = (uv - float2(0.58, 0.55)) * float2(_ScreenParams.x / _ScreenParams.y, 1);
                col += _Glow.rgb * exp(-dot(d, d) * 3.2) * 0.55;
                col *= lerp(1, 0.72, saturate(length(uv - 0.5) * 1.3));
                // light dither against banding
                col += (frac(sin(dot(uv * _ScreenParams.xy, float2(12.9898, 78.233))) * 43758.5453) - 0.5) / 255;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
