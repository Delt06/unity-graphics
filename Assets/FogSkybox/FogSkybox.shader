Shader "Skybox/Fog Skybox"
{
    Properties
    {
        [MainColor]
        _BaseColor("Main Color", Color) = (1, 1, 1, 1)
        _FogThreshold ("Fog Vertical Threshold", Range(-2, 2)) = 0
        _FogSmoothness ("Fog Smoothness", Range(0, 4)) = 0.1
    }
    SubShader
    {
        Tags { "PreviewType"="Skybox" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;
            };

            struct v2f
            {
                float3 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            half4 _BaseColor;
            half _FogThreshold;
            half _FogSmoothness;

            v2f vert (const appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            inline half4 mix_fog(const half4 frag_color, const half v)
            {
                const half t = smoothstep(_FogThreshold, _FogThreshold + _FogSmoothness, v);
                return lerp(unity_FogColor, frag_color, t); 
            }

            half4 frag (const v2f i) : SV_Target
            {
                half4 frag_color = _BaseColor;
                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                frag_color = mix_fog(frag_color, i.uv.y);
                #endif
                return frag_color;
            }
            ENDCG
        }
    }
}
