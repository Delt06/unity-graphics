Shader "DELTation/Billboard Grass (Instanced)"
{
	Properties
	{
	    [MainTexture]
		_BaseMap ("Texture", 2D) = "white" {}
	    _AlphaClipThreshold("Alpha Clip Threshold", Range(0, 1)) = 0.5
	    _BottomTint ("Bottom Tint", Color) = (1, 1, 1, 1)
	    _TopTint ("Top Tint", Color) = (1, 1, 1, 1)
	    _TopThreshold ("Top Threshold", Range(0, 1)) = 0.5
	    _MinDiffuse("Min Diffuse", Range(0, 1)) = 0.5
	    _WindStrength("Wind Strength", Float) = 1
	    _WindScrollVelocity("Wind Scroll Velocity", Vector) = (1, 1, 0, 0)
	    _WindTexture("Wind Texture", 2D) = "normal" {} 
	    _FixedNormal ("Fixed Normal", Vector) = (0, 1, 0, 0)
	}
	SubShader
	{
		Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
	    Cull Off
	    
	    HLSLINCLUDE
	    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
	    ENDHLSL

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
            
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
			#pragma instancing_options assumeuniformscaling
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

			struct appdata
			{
				float3 vertex : POSITION;
				float2 uv : TEXCOORD0;
			    float3 normal : NORMAL;
			    UNITY_VERTEX_INPUT_INSTANCE_ID
			    
			};

			struct v2f
			{
				float3 uv_fog : TEXCOORD0;
				float4 vertex : SV_POSITION;
			    float3 normal_ws : TEXCOORD1;
			    float3 position_ws : TEXCOORD2;
			    UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			half _AlphaClipThreshold;
            half4 _BottomTint;
            half4 _TopTint;
            half _TopThreshold;
            half _MinDiffuse;
			
			float3 _FixedNormal;

            TEXTURE2D(_BaseMap);
			SAMPLER(sampler_BaseMap);

			#include "./GrassWind.hlsl"
			
			v2f vert (const appdata v)
			{
			    v2f o;
			    
			    UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                const float3 position_ws = TransformObjectToWorld(v.vertex);
                o.position_ws = apply_wind(position_ws, v.vertex);

                o.vertex = TransformWorldToHClip(o.position_ws);
			    o.normal_ws = TransformObjectToWorldNormal(_FixedNormal);
				o.uv_fog.xy = v.uv;
			    o.uv_fog.z = ComputeFogFactor(o.vertex.z);
				return o;
			}
			
			half4 frag (const v2f i) : SV_Target
			{
                const half2 uv = i.uv_fog.xy;
				const half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) *
				    lerp(_BottomTint, _TopTint, smoothstep(0, _TopThreshold, uv.y));
			    
			    clip(albedo.a - _AlphaClipThreshold);
                const float4 shadow_coords = TransformWorldToShadowCoord(i.position_ws);
			    Light light = GetMainLight(shadow_coords);
			    light.shadowAttenuation = lerp(light.shadowAttenuation, 1, GetShadowFade(i.position_ws));
			    const half3 normal_ws = i.normal_ws;
			    const half n_dol_l = max(_MinDiffuse, dot(normal_ws, light.direction));
                const half3 diffuse = n_dol_l * albedo.rgb * light.color * light.shadowAttenuation;
			    
			    half3 col = diffuse;
			    col = MixFog(col, i.uv_fog.z);
				return half4(col, 1);
			}
			ENDHLSL
		}
	    
	    Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            
            #pragma multi_compile_instancing

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            float3 _LightDirection;
            float _AlphaClipThreshold;
            TEXTURE2D(_BaseMap);
			SAMPLER(sampler_BaseMap);

            #include "./GrassWind.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 texcoord     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
            };

            float4 GetShadowPositionHClip(Attributes input)
            {
                const float3 positionOS = input.positionOS.xyz;
                float3 positionWS = TransformObjectToWorld(positionOS);
                positionWS = apply_wind(positionWS, positionOS);
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

                output.uv = input.texcoord;
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a - _AlphaClipThreshold);
                return 0;
            }
            
            ENDHLSL
        }
	}
}