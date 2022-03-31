#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "./GrassWindInput.hlsl"

void Unity_RotateAboutAxis_Radians_float(float3 In, float3 Axis, float Rotation, out float3 Out)
{
    float s = sin(Rotation);
    float c = cos(Rotation);
    float one_minus_c = 1.0 - c;

    Axis = SafeNormalize(Axis);
    float3x3 rot_mat = 
    {   one_minus_c * Axis.x * Axis.x + c, one_minus_c * Axis.x * Axis.y - Axis.z * s, one_minus_c * Axis.z * Axis.x + Axis.y * s,
        one_minus_c * Axis.x * Axis.y + Axis.z * s, one_minus_c * Axis.y * Axis.y + c, one_minus_c * Axis.y * Axis.z - Axis.x * s,
        one_minus_c * Axis.z * Axis.x - Axis.y * s, one_minus_c * Axis.y * Axis.z + Axis.x * s, one_minus_c * Axis.z * Axis.z + c
    };
    Out = mul(rot_mat,  In);
}

float3 apply_wind(float3 position_ws, float3 position_os)
{
    const float3 pivot = TransformObjectToWorld(0);
    float3 offset_from_pivot = position_ws - pivot;

    const float2 wind_uv = (position_ws.xz + _Time.y * _WindScrollVelocity) * _WindTexture_ST.xy + _WindTexture_ST.zw;
    float2 axis2d = SAMPLE_TEXTURE2D_LOD(_WindTexture, sampler_WindTexture, wind_uv, 0).xy;
    axis2d = axis2d * 2 - 1;
    const float3 axis = float3(axis2d.x, 0, axis2d.y);
    const float angle = _WindStrength * max(0, position_os.y) * length(axis);

    Unity_RotateAboutAxis_Radians_float(offset_from_pivot, axis, angle, offset_from_pivot);
    position_ws = pivot + offset_from_pivot;
    return position_ws;
}