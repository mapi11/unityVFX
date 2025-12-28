Shader "TrailRT/Splat"
{
    Properties
    {
        _SplatUV ("Splat UV", Vector) = (0.5,0.5,0,0)
        _Radius ("Radius", Float) = 0.05
        _Strength ("Strength", Float) = 1
        _Hardness ("Hardness", Float) = 4
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Pass
        {
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _SplatUV;
            float _Radius;
            float _Strength;
            float _Hardness;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float baseVal = tex2D(_MainTex, i.uv).r;

                float d = distance(i.uv, _SplatUV.xy);
                float t = saturate(1.0 - d / max(1e-5, _Radius));
                t = pow(t, max(0.0001, _Hardness)) * _Strength;

                float outVal = saturate(max(baseVal, t)); // можно заменить на baseVal + t
                return fixed4(outVal, outVal, outVal, 1);
            }
            ENDHLSL
        }
    }
}
