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
			    half2 matcap_uv : TEXCOORD1;
			};

			CBUFFER_START(UnityPerMaterial)

			half4 _BaseColor;
			
			TEXTURE2D(_BaseMap);
			SAMPLER(sampler_BaseMap);
			float4 _BaseMap_ST;

			TEXTURE2D(_MatCap);
			SAMPLER(sampler_MatCap);
			
			CBUFFER_END
			
			v2f vert (const appdata v)
			{
				v2f o;
			    
				o.vertex = TransformObjectToHClip(v.vertex.xyz);
				o.uv = TRANSFORM_TEX(v.uv, _BaseMap);

			    half2 matcap_uv = TransformWorldToViewDir(TransformObjectToWorldDir(v.normal)).xy;
			    matcap_uv = matcap_uv * 0.5 + 0.5; // remap from [-1; 1] to [0; 1]
			    o.matcap_uv = matcap_uv;
			    
				return o;
			}
			
			half4 frag (const v2f i) : SV_Target
			{
			    const half4 matcap_sample = SAMPLE_TEXTURE2D(_MatCap, sampler_MatCap, i.matcap_uv);
			    const half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
			    return matcap_sample * albedo;
			}
			ENDHLSL
		}
	}
}