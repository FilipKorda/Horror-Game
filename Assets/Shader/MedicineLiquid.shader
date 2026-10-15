Shader "UI/MedicineLiquid"
{
    Properties
    {
        [PerRendererData] _MainTex ("Plus Mask (alpha)", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Liquid)]
        _LiquidColor ("Liquid Color", Color) = (1, 0.52, 0.2, 1)
        _DeepColor ("Deep Color", Color) = (0.78, 0.25, 0.08, 1)
        _RimGlow ("Surface Glow", Range(0, 1)) = 0.3
        _SurfaceSoftness ("Surface Softness", Range(0.001, 0.1)) = 0.02

        [Header(Waves)]
        _WaveFrequency ("Wave Frequency", Float) = 1.5
        _WaveAmplitude ("Wave Amplitude", Range(0, 0.3)) = 0.1
        _Tilt ("Tilt", Range(0, 1)) = 0.35

        [HideInInspector] _Level ("Level", Range(0, 1)) = 0
        [HideInInspector] _Slosh ("Slosh", Range(0, 1)) = 0
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
            fixed4 _DeepColor;
            float _RimGlow;
            float _SurfaceSoftness;
            float _WaveFrequency;
            float _WaveAmplitude;
            float _Tilt;
            float _Level;
            float _Slosh;
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
                float mask = tex2D(_MainTex, uv).a;

                // Fale gasną przy pustej i pełnej fiolce, żeby poziom 0 i 1 był zawsze czysty.
                float edge = saturate(min(_Level, 1.0 - _Level) * 10.0);
                float amplitude = _Slosh * _WaveAmplitude * edge;

                float wave = sin(uv.x * _WaveFrequency * 6.2831853 + _Phase)
                           + 0.5 * sin(uv.x * _WaveFrequency * 11.0 - _Phase * 1.3);
                float tilt = (uv.x - 0.5) * sin(_Phase * 0.6) * _Slosh * _Tilt * edge;
                float surface = _Level + wave * amplitude + tilt;

                float liquid = 1.0 - smoothstep(surface - _SurfaceSoftness, surface, uv.y);
                liquid = saturate(liquid + step(0.999, _Level));

                float depth = saturate(uv.y / max(surface, 0.05));
                float rim = saturate(1.0 - (surface - uv.y) / 0.08) * liquid;
                float3 rgb = saturate(lerp(_DeepColor.rgb, _LiquidColor.rgb, depth) + rim * _RimGlow);

                half4 color = half4(rgb, liquid * mask) * IN.color;

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
