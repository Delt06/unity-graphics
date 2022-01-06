using System;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace GeometryGrass
{
    [ExecuteAlways]
    public class GeometryGrassMeshGenerator : MonoBehaviour
    {
        private static readonly VertexAttributeDescriptor[] VertexAttributeDescriptors =
        {
            new VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.Position),
            new VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.Normal),
            new VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.Tangent, VertexAttributeFormat.Float32,
                4
            ),
        };
        [SerializeField] private float2 _size = new float2(0, 0) * 10f;
        [SerializeField] [Min(0f)] private float _density = 1f;

        private Mesh _mesh;
        private MeshFilter _meshFilter;

        private void LateUpdate()
        {
            if (!_mesh)
                _mesh = new Mesh();
            _mesh.Clear();
            if (!_meshFilter)
                _meshFilter = GetComponent<MeshFilter>();


            var counts = (int2) math.round(_size * _density);
            var verticesCount = counts.x * counts.y;
            var vertexBuffer = new NativeArray<VertexAttribute>(verticesCount, Allocator.Temp,
                NativeArrayOptions.UninitializedMemory
            );
            var indices = new NativeArray<int>(verticesCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            var boundsSize = new float3(_size.x, 0, _size.y);

            for (var i = 0; i < verticesCount; i++)
            {
                var xi = i % counts.x;
                var yi = i / counts.x;
                var xt = (float) xi / (counts.x - 1);
                var yt = (float) yi / (counts.y - 1);
                vertexBuffer[i] = new VertexAttribute
                {
                    Vertex = math.lerp(0, boundsSize, new float3(xt, 0, yt)) - boundsSize * 0.5f,
                    Normal = new float3(0, 1, 0),
                    Tangent = new float4(0, 0, 1, 1),
                };
                indices[i] = i;
            }

            _mesh.SetVertexBufferParams(verticesCount, VertexAttributeDescriptors);
            _mesh.SetVertexBufferData(vertexBuffer, 0, 0, verticesCount, flags: MeshUpdateFlags.DontRecalculateBounds);
            _mesh.SetIndices(indices, MeshTopology.Points, 0, false);
            _mesh.bounds = new Bounds(Vector3.zero, boundsSize);
            _meshFilter.sharedMesh = _mesh;

            vertexBuffer.Dispose();
            indices.Dispose();
        }

        private void OnDestroy()
        {
            if (_mesh)
            {
                CoreUtils.Destroy(_mesh);
                _mesh = null;
            }
        }

        [Serializable]
        [SuppressMessage("ReSharper", "NotAccessedField.Global")]
        public struct VertexAttribute
        {
            public float3 Vertex;
            public float3 Normal;
            public float4 Tangent;
        }
    }
}