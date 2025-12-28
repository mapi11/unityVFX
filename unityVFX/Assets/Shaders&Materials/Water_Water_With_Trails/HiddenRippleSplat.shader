Shader "Hidden/RippleSplat"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" }
        Pass
        {
            Name "RippleSplat"
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

            float4 _SplatParams; // x=u, y=v, z=radius(uv), w=strength (height delta)
            float  _Hardness;    // 0..1 (насколько резкий край)

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

                float2 center = _SplatParams.xy;
                float  radius = max(_SplatParams.z, 1e-5);
                float  strength = _SplatParams.w;

                float d = distance(i.uv, center);
                float t = saturate(1.0 - d / radius);

                // Ум€гкостьФ кра€: hardness=0 -> м€гко, hardness=1 -> резко
                float edge = ( _Hardness >= 0.999 ) ? step(0.5, t) : smoothstep(0.0, 1.0, pow(t, lerp(1.0, 8.0, _Hardness)));

                // волна вокруг 0.5: + и -
                float wave = 0.5 + 0.5 * cos(d * 6.2831853 / radius);
                float delta = (wave - 0.5) * strength * edge;

                half v = prev + (half)delta;
                return half4(v, v, v, 1.0h);
            }
            ENDHLSL
        }
    }
}
