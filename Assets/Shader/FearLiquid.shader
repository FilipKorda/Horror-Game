Shader "UI/FearLiquid"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Liquid)]
        _LiquidColor ("Liquid Color", Color) = (0.8, 0.12, 0.4, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.95
        _DeepFactor ("Deep Brightness", Range(0, 1)) = 0.28
        _RimGlow ("Surface Glow", Range(0, 2)) = 0.8
        _SurfaceSoftness ("Surface Softness", Range(0.001, 0.05)) = 0.008

        [Header(Vessel)]
        _Radius ("Liquid Radius (fraction of half size)", Range(0.1, 1)) = 0.8636

        [Header(Flow)]
        _FlowScale ("Flow Scale", Float) = 3
        _WaveFrequency ("Surface Wave Frequency", Float) = 2.2
        _IdleAmplitude ("Idle Wave Amplitude", Range(0, 0.1)) = 0.014
        _AgitationAmplitude ("Agitation Wave Amplitude", Range(0, 0.2)) = 0.035

        [HideInInspector] _Level ("Level", Range(0, 1)) = 0
        [HideInInspector] _Agitation ("Agitation", Range(0, 1)) = 0
        [HideInInspector] _Phase ("Phase", Float) = 0

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _ClipRect;

            fixed4 _LiquidColor;
            float _Opacity;
            float _DeepFactor;
            float _RimGlow;
            float _SurfaceSoftness;
            float _Radius;
            float _FlowScale;
            float _WaveFrequency;
            float _IdleAmplitude;
            float _AgitationAmplitude;
            float _Level;
            float _Agitation;
            float _Phase;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float2 p = uv * _FlowScale;

                float2 centered = (uv - 0.5) * 2.0;
                float dist = length(centered);
                float circle = 1.0 - smoothstep(_Radius - 0.012, _Radius, dist);

                // Poziom to wysokość między dnem a górą koła. Faza jest zawijana do 2π, a mnożniki fazy są
                // całkowite, więc zawinięcie nie daje skoku.
                float edge = saturate(min(_Level, 1.0 - _Level) * 12.0);
                float amplitude = (_IdleAmplitude + _Agitation * _AgitationAmplitude) * edge;
                float wave = sin(uv.x * 6.2831853 * _WaveFrequency + _Phase)
                           + 0.55 * sin(uv.x * 6.2831853 * _WaveFrequency * 2.3 - 2.0 * _Phase);
                float surface = 0.5 - 0.5 * _Radius + _Level * _Radius + wave * amplitude;

                float inside = 1.0 - smoothstep(surface - _SurfaceSoftness, surface, uv.y);
                inside = saturate(inside + step(0.999, _Level));

                float f1 = sin(p.y * 1.9 - _Phase + sin(p.x * 6.0 + _Phase) * 1.1);
                float f2 = sin(p.y * 4.3 + p.x * 2.6 - 2.0 * _Phase + sin(p.y * 1.3 + p.x * 7.0 - _Phase) * 1.4);
                float f3 = sin(p.y * 8.7 - p.x * 5.0 - 3.0 * _Phase);
                float flow = 0.5 + 0.30 * f1 + 0.18 * f2 + 0.07 * f3;

                float sphere = sqrt(saturate(1.0 - dist * dist / (_Radius * _Radius)));
                float behind = max(surface - uv.y, 0.0);
                float lead = exp(-behind * 5.0);

                float shade = saturate(0.22 + 0.32 * flow + 0.30 * sphere + 0.22 * lead);
                float3 bright = lerp(_LiquidColor.rgb, float3(1.0, 1.0, 1.0), 0.55);
                float3 rgb = _LiquidColor.rgb * lerp(_DeepFactor, 1.0, shade);

                float glint = smoothstep(0.82, 1.0, 0.5 * (f1 + f2));
                rgb += bright * (exp(-behind * 55.0) * _RimGlow + glint * 0.35 * sphere);

                half4 color = half4(saturate(rgb), inside * circle * _Opacity) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
