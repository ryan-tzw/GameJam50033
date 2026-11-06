Shader "Jam50033/Hazard Brick"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        _BaseColor ("Brick Color", Color) = (0.90196,0.36078,0.38824,1)
        _MarkColor ("Exclamation Color", Color) = (0.03,0.015,0.02,1)
        [HDR] _GlowColor ("Outline Color", Color) = (1,0.45,0.08,1)
        _OutlineWidth ("Outline Width", Range(0.005,0.15)) = 0.055
        _GlowStrength ("Emission", Float) = 2.5
        _SymbolSize ("Symbol Size", Range(0.2,1)) = 0.95
        [HDR] _SymbolColor ("Symbol Color", Color) = (1,0.8,0.15,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
            };
            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _BaseColor;
                half4 _MarkColor;
                half4 _GlowColor;
                half4 _SymbolColor;
                float _OutlineWidth;
                float _GlowStrength;
                float _SymbolSize;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            float SegmentDistance(float2 p, float2 a, float2 b)
            {
                float2 edge = b - a;
                return length(p - a - edge * saturate(dot(p - a, edge) / dot(edge, edge)));
            }

            float Cross2D(float2 a, float2 b)
            {
                return a.x * b.y - a.y * b.x;
            }

            float RoundedTriangleDistance(float2 p)
            {
                float2 a = float2(0.5, 0.8);
                float2 b = float2(0.2, 0.24);
                float2 c = float2(0.8, 0.24);
                float distance = min(SegmentDistance(p, a, b),
                    min(SegmentDistance(p, b, c), SegmentDistance(p, c, a)));
                bool inside = Cross2D(b-a, p-a) >= 0 && Cross2D(c-b, p-b) >= 0 && Cross2D(a-c, p-c) >= 0;
                // Expanding the triangle by a small radius rounds its corners.
                return (inside ? -distance : distance) - 0.025;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _BaseColor;
                // Hazard colour is independent of the piece tint; preserve the renderer's opacity.
                sprite.a *= input.color.a;
                float2 edgeDistance = min(input.uv, 1 - input.uv);
                float outline = 1 - step(_OutlineWidth, min(edgeDistance.x, edgeDistance.y));
                half3 color = lerp(sprite.rgb, _GlowColor.rgb * _GlowStrength, outline);

                float2 p = (input.uv - 0.5) / max(_SymbolSize, 0.01) + 0.5;
                float triangleDistance = RoundedTriangleDistance(p);
                float aa = max(fwidth(p.x), fwidth(p.y)) * 0.5;
                float fill = 1 - smoothstep(-aa, aa, triangleDistance);
                float stem = SegmentDistance(p, float2(0.5, 0.45), float2(0.5, 0.62)) - 0.035;
                float dotDistance = length(p - float2(0.5, 0.34)) - 0.035;
                float mark = 1 - smoothstep(-aa, aa, min(stem, dotDistance));
                float symbol = fill * _SymbolColor.a;
                color = lerp(color, _SymbolColor.rgb * _GlowStrength, symbol);
                // Keep the mark dark through the entire glow pulse for a readable silhouette.
                color = lerp(color, _MarkColor.rgb, mark * fill * _MarkColor.a);
                return half4(color, sprite.a);
            }
            ENDHLSL
        }
    }
}
