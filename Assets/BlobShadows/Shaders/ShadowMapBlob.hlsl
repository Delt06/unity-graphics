#ifndef SHADOW_MAP_BLOB
#define SHADOW_MAP_BLOB

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_ShadowMapBlob);
SAMPLER(sampler_ShadowMapBlob);

float4 _ShadowMapPlanarParams;

inline half sample_blob_shadow_map(float3 position_ws)
{
    const half2 size = _ShadowMapPlanarParams.xy;
    const half2 offset = _ShadowMapPlanarParams.zw;
    const half2 uv = (position_ws.xz - offset) / size + 0.5;
    const half2 cancel_factors = step(0.5, abs(uv - 0.5));
    const half shadow_map_sample = 1 - SAMPLE_TEXTURE2D_LOD(_ShadowMapBlob, sampler_ShadowMapBlob, uv, 0).r;
    return saturate(
        shadow_map_sample +
        cancel_factors.x +
        cancel_factors.y
    );
}

#endif
