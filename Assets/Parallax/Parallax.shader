Shader "Unlit/Parallax"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Parallax ("Parallax", Float) = 0.1
        [NoScaleOffset]
        _NoiseTex ("Noise Texture", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 position_os : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal_os : NORMAL;
                float4 tangent_os : TANGENT;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 position_cs : SV_POSITION;
                float3 view_dir_ts : VIEW_DIR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float _Parallax;

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            v2f vert (appdata v)
            {
                v2f o;
                const float3 position_ws = TransformObjectToWorld(v.position_os.xyz);
                o.position_cs = TransformWorldToHClip(position_ws);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                const float3 normal_ws = TransformObjectToWorldDir(v.normal_os);
                const float3 tangent_ws = TransformObjectToWorldDir(v.tangent_os.xyz);
                const float3 binormal_ws = cross (normal_ws, tangent_ws.xyz) * v.tangent_os.w;
                const float3x3 tangent_to_world = float3x3(tangent_ws.xyz, binormal_ws, normal_ws);
                const float3 view_dir = GetWorldSpaceViewDir(position_ws);
                const float3 view_dir_ts = mul(tangent_to_world, view_dir);
                o.view_dir_ts = normalize(view_dir_ts);
                
                return o;
            }

            float4 sample_color(const float depth_normalized, float2 uv, const float3 view_dir_ts)
            {
                uv = uv - view_dir_ts.xy * depth_normalized;
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
            }

            #define SAMPLES 250
            #define ONE_OVER_SAMPLES 1.0 / SAMPLES;

            float4 frag (const v2f i) : SV_Target
            {
                float depth = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.uv).r * _Parallax;
                return sample_color(depth, i.uv, i.view_dir_ts);
            }
            ENDHLSL
        }
    }
}
