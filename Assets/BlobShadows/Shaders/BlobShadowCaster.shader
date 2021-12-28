Shader "Hidden/Blob Shadows/Caster"
{
    Properties
    {
        _Threshold ("Threshold", Range(-0.5, 1)) = 0
        _Smoothness ("Smoothness", Range(0.001, 1)) = 1
    }
    SubShader
    {
        ZTest Always
        ZWrite Off
        ColorMask R
        Blend SrcColor One
        BlendOp Add

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                half color : COLOR; // x=power
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                half power : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial);
            half _Threshold;
            half _Smoothness;
            half _Power;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                o.power = v.color.r;
                return o;
            }

            inline half general_length(const half2 v, const half power)
            {
                return pow(pow(v.x, power) + pow(v.y, power), 1 / power);
            }

            half4 frag (const v2f i) : SV_Target
            {
                const half2 duv = abs(i.uv - 0.5);
                const half length = general_length(duv, i.power);
                const half attenuation = smoothstep(_Threshold, _Threshold + _Smoothness, length);
                return half4(1 - attenuation, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}
