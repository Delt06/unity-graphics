Shader "Custom/Toon Terrain"
{
    Properties
    {
        [HideInInspector] _Control ("Control (RGBA)", 2D) = "red" {}

        [HideInInspector] _Splat0 ("Layer 0 (R)", 2D) = "white" {}
        [HideInInspector] _Splat1 ("Layer 1 (G)", 2D) = "white" {}
        [HideInInspector] _Splat2 ("Layer 2 (B)", 2D) = "white" {}
        [HideInInspector] _Splat3 ("Layer 3 (A)", 2D) = "white" {}
        
        _Ramp ("Ramp", 2D) = "white" {}
        _RampOpacity ("Ramp Opacity", Range(0, 1)) = 1 
    }
    SubShader
    {
        Pass
        {
            Tags { "RenderType"="Opaque" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv_control : TEXCOORD0;
                float4 uv_splat01 : TEXCOORD1;
                float4 uv_splat23 : TEXCOORD2;
                float3 normal_ws : TEXCOORD3;
                
                #ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
                float4 shadow_coord : TEXCOORD4;
                #endif

                float3 position_ws : TEXCOORD5;
            };

            CBUFFER_START(UnityPerMaterial)
            
            float4 _Control_ST;
            float4 _Splat0_ST;
            float4 _Splat1_ST;
            float4 _Splat2_ST;
            float4 _Splat3_ST;

            TEXTURE2D(_Ramp); SAMPLER(sampler_Ramp);
            half _RampOpacity;
            
            CBUFFER_END

            TEXTURE2D(_Control); SAMPLER(sampler_Control);
            TEXTURE2D(_Splat0); SAMPLER(sampler_Splat0);
            TEXTURE2D(_Splat1); SAMPLER(sampler_Splat1);
            TEXTURE2D(_Splat2); SAMPLER(sampler_Splat2);
            TEXTURE2D(_Splat3); SAMPLER(sampler_Splat3);

            

            inline float2 apply_tiling_offset(const float2 uv, const float4 tiling_offset)
            {
                return uv * tiling_offset.xy + tiling_offset.zw;
            }

            v2f vert (const appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                
                const float3 position_ws = TransformObjectToWorld(v.vertex.xyz);
                o.vertex = TransformWorldToHClip(position_ws);
                o.uv_control = apply_tiling_offset(v.uv, _Control_ST);
                o.uv_splat01.xy = apply_tiling_offset(v.uv, _Splat0_ST);
                o.uv_splat01.zw = apply_tiling_offset(v.uv, _Splat1_ST);
                o.uv_splat23.xy = apply_tiling_offset(v.uv, _Splat2_ST);
                o.uv_splat23.zw = apply_tiling_offset(v.uv, _Splat3_ST);
                o.normal_ws = TransformObjectToWorldDir(v.normal);
                
                
                
                #ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
                o.shadow_coord = TransformWorldToShadowCoord(position_ws);
                #endif

                o.position_ws = position_ws;
                
                return o;
            }

            half4 frag (const v2f i) : SV_Target
            {
                half4 splat_control = SAMPLE_TEXTURE2D(_Control, sampler_Control, i.uv_control);
                half3 albedo = splat_control.r * SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, i.uv_splat01.xy).rgb;
                albedo += splat_control.g * SAMPLE_TEXTURE2D(_Splat1, sampler_Splat1, i.uv_splat01.zw).rgb;
                albedo += splat_control.b * SAMPLE_TEXTURE2D(_Splat2, sampler_Splat2, i.uv_splat23.xy).rgb;
                albedo += splat_control.a * SAMPLE_TEXTURE2D(_Splat3, sampler_Splat3, i.uv_splat23.zw).rgb;
                const half3 normal_ws = normalize(i.normal_ws);

                #ifdef MAIN_LIGHT_CALCULATE_SHADOWS
                const float4 shadow_coord = TransformWorldToShadowCoord(i.position_ws);
                #elif defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                const float4 shadow_coord = i.shadow_coord; // main light shadows bug
                #else
                const float4 shadow_coord = 0;
                #endif
                
                Light light = GetMainLight(shadow_coord);
                light.shadowAttenuation = lerp(light.shadowAttenuation, 1, GetShadowFade(i.position_ws));
                half attenuation = dot(light.direction, normal_ws);
                attenuation = min(attenuation * light.shadowAttenuation * light.distanceAttenuation, attenuation);
                half3 ramp_sample = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, half2(saturate((attenuation + 1) * 0.5), 0.5)).rgb;
                ramp_sample = lerp(1, ramp_sample, _RampOpacity);
                half3 diffuse = albedo * ramp_sample * light.color;
                return half4(diffuse, 1);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags {"LightMode" = "ShadowCaster"}
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
            #endif

                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            
            ENDHLSL
        }
    }
    
}
