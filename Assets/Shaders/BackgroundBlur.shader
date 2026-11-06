Shader "Jam50033/Background Blur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        _BlurRadius ("Blur Radius (screen pixels)", Range(0,8)) = 0
        [HideInInspector] _TileHorizontally ("Tile Horizontally", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One OneMinusSrcAlpha
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
                float _BlurRadius;
                float _TileHorizontally;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }
            half4 ReadPremultiplied(float2 uv)
            {
                // Wrap across horizontal tile edges, but keep the top and bottom from bleeding together.
                uv.x = _TileHorizontally > 0.5 ? frac(uv.x) : saturate(uv.x);
                uv.y = saturate(uv.y);
                half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, uv);
                texel.rgb *= texel.a;
                return texel;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 sprite;
                if (_BlurRadius <= 0.001)
                {
                    sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                    sprite.rgb *= sprite.a;
                }
                else
                {
                    // Screen derivatives keep the blur equally soft across different source resolutions.
                    float2 stepX = ddx(input.uv) * (_BlurRadius * 0.5);
                    float2 stepY = ddy(input.uv) * (_BlurRadius * 0.5);
                    static const float weights[5] = { 1, 4, 6, 4, 1 };
                    sprite = 0;
                    [unroll] for (int y = 0; y < 5; y++)
                    {
                        [unroll] for (int x = 0; x < 5; x++)
                        {
                            sprite += ReadPremultiplied(input.uv + (x - 2) * stepX + (y - 2) * stepY)
                                * (weights[x] * weights[y] / 256.0);
                        }
                    }
                }

                // Premultiplied sampling avoids dark fringes around transparent clouds and waves.
                sprite.rgb *= input.color.rgb * input.color.a;
                sprite.a *= input.color.a;
                return sprite;
            }
            ENDHLSL
        }
    }
}
