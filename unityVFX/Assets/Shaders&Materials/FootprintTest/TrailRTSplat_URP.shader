Shader "Hidden/TrailRT/Splat_URP"
{
    Properties
    {
        _BlitTexture ("Blit Texture", 2D) = "black" {}
        _SplatUV ("Splat UV", Vector) = (0.5,0.5,0,0)
        _Radius ("Radius (UV)", Float) = 0.05
        _Strength ("Strength", Float) = 1
        _Hardness ("Hardness", Float) = 4
        _FlipY ("FlipY", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend One Zero

        Pass
        {
            Name "SPLAT"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            float4 _SplatUV;
            float _Radius;
            float _Strength;
            float _Hardness;
            float _FlipY;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert (Attributes i)
            {
                Varyings o;
                o.positionHCS = GetFullScreenTriangleVertexPosition(i.vertexID);
                o.uv = GetFullScreenTriangleTexCoord(i.vertexID);
                return o;
            }

            half4 Frag (Varyings i) : SV_Target
            {
                float2 uv = i.uv;

                // ВАЖНО: выравниваем UV для RenderTexture Blit на D3D/URP
                if (_FlipY > 0.5)
                    uv.y = 1.0 - uv.y;

                half baseV = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv).r;

                float2 d = (uv - _SplatUV.xy);
                float dist = length(d);

                float r = max(_Radius, 1e-5);
                float t = saturate(1.0 - (dist / r));
                // hardness: чем больше, тем резче край
                float mask = pow(t, _Hardness);

                half addV = (half)(mask * _Strength);

                // делаем след устойчивым (без “грязных” сумм)
                half outV = max(baseV, saturate(addV));

                return half4(outV, 0, 0, 1);
            }
            ENDHLSL
        }
    }
}
