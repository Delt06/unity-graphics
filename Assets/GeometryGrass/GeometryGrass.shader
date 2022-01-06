Shader "DELTation/Geometry Grass"
{
    Properties
    {
        _BendRotationRandom("Bend Rotation Random", Range(0, 1)) = 0.2
        _BladeWidth("Blade Width", Float) = 0.05
        _BladeWidthRandom("Blade Width Random", Float) = 0.02
        _BladeHeight("Blade Height", Float) = 0.5
        _BladeHeightRandom("Blade Height Random", Float) = 0.3
        
        _BladeForward("Blade Forward Amount", Float) = 0.38
        _BladeCurve("Blade Curvature Amount", Range(1, 4)) = 2
        
        _ColorBottom("Color Bottom", Color) = (0, 0.5, 0, 1)
        _ColorTop("Color Top", Color) = (0, 1, 0, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geo
            #pragma fragment frag

            #define BLADE_SEGMENTS 3

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Simple noise function, sourced from http://answers.unity.com/answers/624136/view.html
	        // Extended discussion on this function can be found at the following link:
	        // https://forum.unity.com/threads/am-i-over-complicating-this-random-function.454887/#post-2949326
	        // Returns a number in the 0...1 range.
	        float rand(float3 co)
	        {
		        return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
	        }

	        // Construct a rotation matrix that rotates around the provided axis, sourced from:
	        // https://gist.github.com/keijiro/ee439d5e7388f3aafc5296005c8c3f33
	        float3x3 angle_axis_3x3(const float angle, float3 axis)
	        {
		        float c, s;
		        sincos(angle, s, c);

                const float t = 1 - c;
                const float x = axis.x;
                const float y = axis.y;
                const float z = axis.z;

		        return float3x3(
			        t * x * x + c, t * x * y - s * z, t * x * z + s * y,
			        t * x * y + s * z, t * y * y + c, t * y * z - s * x,
			        t * x * z - s * y, t * y * z + s * x, t * z * z + c
			        );
	        }

            CBUFFER_START(UnityPerMaterial)
            half _BendRotationRandom;
            half _BladeHeight;
            half _BladeHeightRandom;	
            half _BladeWidth;
            half _BladeWidthRandom;

            half _BladeForward;
            half _BladeCurve;
            
            half3 _ColorBottom;
            half3 _ColorTop;
            
            CBUFFER_END

            struct vertex_input
            {
                float4 vertex_os : POSITION;
                float3 normal_os : NORMAL;
                float4 tangent_os : TANGENT;
            };

            struct vertex_output
            {
                float4 vertex_ws : SV_POSITION;
	            float3 normal_ws : NORMAL;
	            float4 tangent_ws : TANGENT;
            };

            struct geometry_output
            {
                float2 uv : TEXCOORD0;
                float4 position_cs : SV_POSITION;
            };

            vertex_output vert (const vertex_input v)
            {
                vertex_output o;
                o.vertex_ws = float4(TransformObjectToWorld(v.vertex_os.xyz), 1);
                o.normal_ws = TransformObjectToWorldNormal(v.normal_os);
                o.tangent_ws = float4(TransformObjectToWorldNormal(v.tangent_os.xyz), v.tangent_os.w);
                return o;
            }

            

            inline geometry_output geo_output(const float3 position_ws, const float2 uv)
            {
                geometry_output o;
                o.position_cs = TransformWorldToHClip(position_ws);
                o.uv = uv;
                return o;
            }

            inline geometry_output generate_geo_output(const float3 vertex_ws, const float width, const float height, const float forward, const float2 uv, const float3x3 transform_matrix)
            {
                const float3 tangent_point = float3(width, forward, height);
                const float3 position_ws = vertex_ws + mul(transform_matrix, tangent_point);
	            return geo_output(position_ws, uv);
            }

            [maxvertexcount(BLADE_SEGMENTS * 2 + 1)]
            void geo(point vertex_output IN[1], inout TriangleStream<geometry_output> tri_stream)
            {
                vertex_output input = IN[0];
                const float3 vertex_ws = input.vertex_ws.xyz;
                const float3 normal_ws = input.normal_ws;
                const float4 tangent_ws = input.tangent_ws;
                const float3 binormal_ws = cross(normal_ws, tangent_ws.xyz) * tangent_ws.w;

                const float3x3 facing_rotation_matrix = angle_axis_3x3(rand(vertex_ws) * TWO_PI, float3(0, 0, 1));
                const float3x3 bend_rotation_matrix = angle_axis_3x3(rand(vertex_ws.zzx) * _BendRotationRandom * PI * 0.5, float3(-1, 0, 0));

                const float3x3 tangent_to_world = float3x3(
	                tangent_ws.x, binormal_ws.x, normal_ws.x,
	                tangent_ws.y, binormal_ws.y, normal_ws.y,
	                tangent_ws.z, binormal_ws.z, normal_ws.z
	            );

                const float3x3 transformation_facing_matrix = mul(tangent_to_world, facing_rotation_matrix);
                const float3x3 transformation_matrix = mul(transformation_facing_matrix, bend_rotation_matrix);

                const float height = (rand(vertex_ws.zyx) * 2 - 1) * _BladeHeightRandom + _BladeHeight;
                const float width = (rand(vertex_ws.xzy) * 2 - 1) * _BladeWidthRandom + _BladeWidth;
                const float forward = rand(vertex_ws.yyz) * _BladeForward;

                UNITY_UNROLL
                for (int i = 0; i < BLADE_SEGMENTS; i++)
                {
                    const float t = i / (float)BLADE_SEGMENTS;
                    const float segment_width = width * (1 - t);
                    const float segment_height = height * t;
                    const float segment_forward = pow(t, _BladeCurve) * forward;

                    const float3x3 m = i == 0 ? transformation_facing_matrix : transformation_matrix;
                    tri_stream.Append(generate_geo_output(vertex_ws, segment_width, segment_height, segment_forward, float2(0, t), m));
                    tri_stream.Append(generate_geo_output(vertex_ws, -segment_width, segment_height, segment_forward, float2(t, t), m));
                }

                tri_stream.Append(generate_geo_output(vertex_ws, 0, height, forward, float2(0.5, 1), transformation_matrix));

                
            }

            half4 frag (geometry_output i, const half facing : VFACE) : SV_Target
            {
                half3 albedo = lerp(_ColorBottom, _ColorTop, i.uv.y);
                return half4(albedo, 1);
            }
            ENDHLSL
        }
    }
}
