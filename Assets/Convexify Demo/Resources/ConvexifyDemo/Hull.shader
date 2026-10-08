// Shades the convex hulls and the source model of the demo.
// Camera-relative key light, sky/ground fill and rim, so the shapes read well from every angle without scene lights.
// Hull meshes carry the hull center in TEXCOORD0 (for the explode offset) and edge coordinates in TEXCOORD1.
Shader "Convexify Demo/Hull" {
    Properties {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _VertexColor ("Use Vertex Color", Float) = 1
        _EdgeColor ("Edge Color", Color) = (0.05, 0.06, 0.08, 0.55)
        _EdgeWidth ("Edge Width (px)", Float) = 1.1
        _Explode ("Explode", Float) = 0
        _ExplodeCenter ("Explode Center", Vector) = (0, 0, 0, 0)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
        _DepthOffset ("Depth Offset", Float) = 0
    }
    SubShader {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass {
            Cull Back
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Offset [_DepthOffset], [_DepthOffset]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _VertexColor;
            fixed4 _EdgeColor;
            float _EdgeWidth;
            float _Explode;
            float4 _ExplodeCenter;

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                float3 center : TEXCOORD0;
                float3 edge : TEXCOORD1;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 edge : TEXCOORD2;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v) {
                v2f o;
                float3 p = v.vertex.xyz + (v.center - _ExplodeCenter.xyz) * _Explode;
                float4 world = mul(unity_ObjectToWorld, float4(p, 1));
                o.pos = mul(UNITY_MATRIX_VP, world);
                o.worldPos = world.xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.edge = v.edge;
                fixed4 c = lerp(fixed4(1, 1, 1, 1), v.color, _VertexColor) * _Color;
            #ifndef UNITY_COLORSPACE_GAMMA
                c.rgb = GammaToLinearSpace(c.rgb);
            #endif
                o.color = c;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                float3 n = normalize(i.worldNormal);
                float3 v = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 camRight = UNITY_MATRIX_V[0].xyz;
                float3 camUp = UNITY_MATRIX_V[1].xyz;
                float3 l = normalize(v + camUp * 0.9 - camRight * 0.6);

                float key = saturate(dot(n, l));
                float fill = lerp(0.18, 0.34, n.y * 0.5 + 0.5);
                float rim = pow(1 - saturate(dot(n, v)), 3) * 0.22;
                float3 h = normalize(l + v);
                float spec = pow(saturate(dot(n, h)), 48) * 0.12;

                float3 col = i.color.rgb * (key * 0.78 + fill) + rim + spec;

                // Edge factor: 1 on a visible edge, 0 inside the face. Edges hidden by the mesh builder have coordinates >= 1.
                float3 w = max(fwidth(i.edge) * _EdgeWidth, 1e-5);
                float3 a = saturate(i.edge / w);
                float e = 1 - min(a.x, min(a.y, a.z));
                col = lerp(col, _EdgeColor.rgb, e * _EdgeColor.a);

                return fixed4(col, i.color.a);
            }
            ENDCG
        }
    }
}
