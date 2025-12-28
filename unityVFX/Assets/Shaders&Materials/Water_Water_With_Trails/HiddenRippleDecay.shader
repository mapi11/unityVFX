Shader "Hidden/RippleDecay"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" }
        Pass
        {
            Name "RippleDecay"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float _Decay; // 0..1 (обычно exp(-k*dt))

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half prev = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).r;
                // Базовая “ровная вода” = 0.5
                half v = 0.5h + (prev - 0.5h) * saturate(_Decay);
                return half4(v, v, v, 1.0h);
            }
            ENDHLSL
        }
    }
}
