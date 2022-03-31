#ifndef GRASS_WIND_INPUT
#define GRASS_WIND_INPUT

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

float _WindStrength;
float _WindSpeed;
float2 _WindScrollVelocity;
			
TEXTURE2D(_WindTexture);
SAMPLER(sampler_WindTexture);
float4 _WindTexture_ST;

#endif