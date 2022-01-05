Shader "DELTation/Layered Grass (Instanced)"
{
    Properties
    {
        _NoiseTex ("Noise", 2D) = "white" {}
        _ClipThreshold ("Clip Threshold", Range(0, 1)) = 0.65
        _Bending ("Normals Straightening", Float) = 250
        _ColorBottom ("Color Bottom", Color) = (0, 0, 0, 1)
        _ColorTop ("Color Top", Color) = (1, 1, 1, 1)
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct appdata
            {
                float3 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float3 tangent : TANGENT;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half3 uv_fog : TEXCOORD0;
                half3 tangent_ws : TEXCOORD1;
                half3 normal_ws : TEXCOORD2;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
            UNITY_DEFINE_INSTANCED_PROP(half, _ClipThreshold)
            UNITY_DEFINE_INSTANCED_PROP(half, _Bending)
            UNITY_DEFINE_INSTANCED_PROP(half3, _ColorBottom)
            UNITY_DEFINE_INSTANCED_PROP(half3, _ColorTop)
            UNITY_DEFINE_INSTANCED_PROP(half3, _ShadowColor)
            UNITY_DEFINE_INSTANCED_PROP(half4, _NoiseTex_ST)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            half4 _Offsets[32];
            float _WindSpeed;
            float _WindMaxDistance;

            v2f vert (const appdata v, uint instance_id: SV_InstanceID)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                const float3 normal_ws = TransformObjectToWorldNormal(v.normal);
                
                const half4 offset = _Offsets[instance_id];
                half vertex_offset = offset.x;
                vertex_offset *= SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, v.uv, 0).r;

                const float3 position_ws = TransformObjectToWorld(v.vertex) + normal_ws * vertex_offset;

                const float4 position_cs = TransformWorldToHClip(position_ws); 
                o.vertex = position_cs;
                half2 uv = v.uv;
                const half4 noise_tex_st = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NoiseTex_ST);
                uv = uv * noise_tex_st.xy + noise_tex_st.zw;

                const float depth = LinearEyeDepth(position_cs.z / position_cs.w, _ZBufferParams);
                const half wind_speed = _WindSpeed * saturate(1 - depth / _WindMaxDistance);
                const half wind_factor = sin(wind_speed * _Time.y);
                o.uv_fog.xy = uv + offset.zw * wind_factor;
                o.normal_ws = normal_ws;
                o.tangent_ws = TransformObjectToWorldNormal(v.tangent);
                o.uv_fog.z = ComputeFogFactor(position_cs.z);
                
                return o;
            }

            inline float sample_noise(const half2 uv, const half2 offset = 0)
            {
                return SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv + offset).r;
            }

            half4 frag (const v2f i, uint instance_id: SV_InstanceID) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                const half uv_offset = 0.01;
                const half2 uv = i.uv_fog.xy;
                const half noise = sample_noise(uv);
                clip(noise - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _ClipThreshold));
                const half dx = (noise - sample_noise(uv, half2(-uv_offset, 0))) / uv_offset;
                const half dy = (noise - sample_noise(uv, half2(0, -uv_offset))) / uv_offset;
                const half3 bitangent_ws = cross(i.normal_ws, i.tangent_ws);
                const half3 normal_ws = normalize(bitangent_ws * dy +  i.tangent_ws * dx + i.normal_ws * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Bending));

                const half3 color_bottom = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _ColorBottom);
                const half3 color_top = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _ColorTop);
                const half3 albedo = lerp(color_bottom, color_top, _Offsets[instance_id].y);

                const Light main_light = GetMainLight();
                const half n_dot_l = saturate(dot(normal_ws, main_light.direction));
                const half3 shadow_color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _ShadowColor);
                const half3 diffuse = lerp(albedo * shadow_color, albedo, n_dot_l) * main_light.color;
                half3 fragment_color = diffuse;
                fragment_color = MixFog(fragment_color, i.uv_fog.z); 
                return half4(fragment_color, 1);
            }
            ENDHLSL
        }
    }
}
