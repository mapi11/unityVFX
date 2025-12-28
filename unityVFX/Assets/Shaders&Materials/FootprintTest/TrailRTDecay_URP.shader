Shader "Hidden/TrailRT/Decay_URP"
{
    Properties
    {
        _BlitTexture ("Blit Texture", 2D) = "black" {}
        _Fade ("Fade", Range(0,1)) = 0.97
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
            Name "DECAY"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            float _Fade;
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

                if (_FlipY > 0.5)
                    uv.y = 1.0 - uv.y;

                half v = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv).r;
                v *= (half)_Fade;

                return half4(v, 0, 0, 1);
            }
            ENDHLSL
        }
    }
}
