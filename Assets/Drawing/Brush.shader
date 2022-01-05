Shader "Unlit/Brush"
{
	Properties
	{
	}
	SubShader
	{
		LOD 100
	    Blend SrcAlpha OneMinusSrcAlpha, One One
	    ZWrite Off
	    ZTest Off
	    Cull Off

		Pass
		{
            HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 uv : TEXCOORD0; // UV + hardness
			    float4 color : COLOR;
			};

			struct v2f
			{
				float3 uv : TEXCOORD0;  // UV + hardness
			    float4 color : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
			    o.uv = v.uv;
				o.color = v.color;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
                const float2 polar_uv = (i.uv.xy - 0.5) * 2;
                const float r = length(polar_uv);
			    const float hardness = i.uv.z;
			    const float alpha = smoothstep(1, hardness - 0.001, r);
			    return i.color * float4(1, 1, 1, alpha);
			}
			ENDHLSL
		}
	}
}