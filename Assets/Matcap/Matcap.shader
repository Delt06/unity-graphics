Shader "Unlit/Matcap"
{
	Properties
	{
	    [MainColor]
	    _BaseColor ("Tint", Color) = (1, 1, 1, 1)
	    [MainTexture]
		_BaseMap ("Texture", 2D) = "white" {}
        [NoScaleOffset]
	    _MatCap ("Matcap", 2D) = "white" {}
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

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			    float3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			    half4 normal_ss : TEXCOORD1; // screen-space normals
			};

			CBUFFER_START(UnityPerMaterial)

			half4 _BaseColor;
			
			TEXTURE2D(_BaseMap);
			SAMPLER(sampler_BaseMap);
			float4 _BaseMap_ST;

			TEXTURE2D(_MatCap);
			SAMPLER(sampler_MatCap);
			
			CBUFFER_END
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = TransformObjectToHClip(v.vertex.xyz);
				o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
			    const half4 normal_cs = half4(TransformWorldToHClipDir(TransformObjectToWorldDir(v.normal)), 0); 
			    o.normal_ss = ComputeScreenPos(normal_cs);
				return o;
			}
			
			half4 frag (v2f i) : SV_Target
			{
			    half2 normal = i.normal_ss.xy;
			    normal = (normal + 1) * 0.5; // remap from [-1; 1] to [0; 1]
			    const half4 matcap_sample = SAMPLE_TEXTURE2D(_MatCap, sampler_MatCap, normal);
			    const half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
			    return albedo * matcap_sample;
			}
			ENDHLSL
		}
	}
}