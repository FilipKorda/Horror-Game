Shader "Hidden/InteractOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Vector) = (4, 4, 4, 1)
        _OutlineWidth ("Outline Width", Float) = 2
        _GlowWidth ("Glow Width", Float) = 12
        _GlowStrength ("Glow Strength", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Mask"

            HLSLPROGRAM
            #pragma vertex MaskVert
            #pragma fragment MaskFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct MaskAttributes
            {
                float3 positionOS : POSITION;
            };

            struct MaskVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            MaskVaryings MaskVert(MaskAttributes input)
            {
                MaskVaryings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                return output;
            }

            half4 MaskFrag(MaskVaryings input) : SV_Target
            {
                return 1;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Composite"

            Blend One One

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment CompositeFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #define TAP_COUNT 12

            float4 _OutlineColor;
            float _OutlineWidth;
            float _GlowWidth;
            float _GlowStrength;

            float Mask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0).r;
            }

            half4 CompositeFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                if (Mask(uv) > 0.5)
                    return 0;

                // Szerokości podane w pikselach przy wysokości 1080, skalowane do aktualnej rozdzielczości
                float2 pixel = (_ScreenParams.zw - 1.0) * (_ScreenParams.y / 1080.0);

                float edge = 0.0;
                float glow = 0.0;

                [unroll]
                for (int i = 0; i < TAP_COUNT; i++)
                {
                    float angle = i * (6.2831853 / TAP_COUNT);
                    float2 direction = float2(cos(angle), sin(angle)) * pixel;

                    edge = max(edge, Mask(uv + direction * _OutlineWidth));
                    glow += Mask(uv + direction * _GlowWidth * 0.5) + Mask(uv + direction * _GlowWidth);
                }

                glow /= TAP_COUNT * 2.0;

                return half4(_OutlineColor.rgb * (edge + glow * _GlowStrength), 0);
            }
            ENDHLSL
        }
    }
}
