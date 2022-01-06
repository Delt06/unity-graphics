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
	}
	SubShader
	{
		Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
	    Cull Off

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
			#pragma instancing_options assumeuniformscaling

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
			    UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			half _AlphaClipThreshold;
            half4 _BottomTint;
            half4 _TopTint;
            half _TopThreshold;
            half _MinDiffuse;

            TEXTURE2D(_BaseMap);
			SAMPLER(sampler_BaseMap);
			
			v2f vert (const appdata v)
			{
			    v2f o;
			    
			    UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
				
				o.vertex = TransformObjectToHClip(v.vertex);
			    o.normal_ws = TransformObjectToWorldNormal(v.normal);
				o.uv_fog.xy = v.uv;
			    o.uv_fog.z = ComputeFogFactor(o.vertex.z);
				return o;
			}
			
			half4 frag (const v2f i, const half facing : VFACE) : SV_Target
			{
                const half2 uv = i.uv_fog.xy;
				const half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) *
				    lerp(_BottomTint, _TopTint, smoothstep(0, _TopThreshold, uv.y));
			    
			    clip(albedo.a - _AlphaClipThreshold);
			    const Light light = GetMainLight();
			    const half3 normal_ws = sign(facing) * i.normal_ws;
			    const half n_dol_l = max(_MinDiffuse, saturate(dot(normal_ws, light.direction)));
                const half3 diffuse = n_dol_l * albedo.rgb * light.color;
			    
			    half3 col = diffuse;
			    col = MixFog(col, i.uv_fog.z);
				return half4(col, 1);
			}
			ENDHLSL
		}
	}
}