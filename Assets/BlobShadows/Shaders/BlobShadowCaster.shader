Shader "Blob Shadows/Caster"
{
    Properties
    {
        [HideInInspector]
        _Threshold ("Threshold", Range(-0.5, 1)) = 0
        [HideInInspector]
        _Smoothness ("Smoothness", Range(0.001, 1)) = 1
        [HideInInspector]
        _SrcBlend ("Src Blend", Float) = 0
        [HideInInspector]
        _DstBlend ("Src Blend", Float) = 0
        [HideInInspector]
        _BlendOp ("Blend Op", Float) = 0
    }
    SubShader
    {
        ZTest Always
        ZWrite Off
        ColorMask R
        
        Blend [_SrcBlend] [_DstBlend]
        BlendOp [_BlendOp]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma multi_compile_fragment _ SDF_CIRCLE SDF_BOX
            

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial);
            half _Threshold;
            half _Smoothness;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                
                return o;
            }

            // https://iquilezles.org/www/articles/distfunctions/distfunctions.htm

            

            inline half circle_sdf(const half2 p, const half r)
            {
                return length(p) - r;
            }

            inline half box_sdf(const half2 p, const half2 b)
            {
              half2 q = abs(p) - b;
              return length(max(q,0.0)) + min(max(q.x,q.y),0.0);
            }

            half4 frag (const v2f i) : SV_Target
            {
                const half2 duv = abs(i.uv - 0.5) * 2;
                const half half_size = 0.5;
                #ifdef SDF_CIRCLE
                const half distance = circle_sdf(duv, half_size);
                #elif SDF_BOX
                const half distance = box_sdf(duv, half_size);
                #else
                const half distance = 0;
                #endif
                
                const half attenuation = smoothstep(_Threshold, _Threshold + _Smoothness, distance);
                return half4(1 - attenuation, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}
