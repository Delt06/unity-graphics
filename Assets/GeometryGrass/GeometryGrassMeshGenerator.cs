using System;
using System.Diagnostics.CodeAnalysis;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
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
            new VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.Tangent, dimension: 4),
        };
        [SerializeField] private float2 _size = new float2(0, 0) * 10f;
        [SerializeField] [Min(0f)] private float _density = 1f;
        private MeshGenerationJob? _activeJob;
        private JobHandle? _jobHandle;

        private Mesh _mesh;
        private MeshFilter _meshFilter;

#if UNITY_EDITOR
        // To avoid flickering in EditMode
        private void LateUpdate()
        {
            if (!Application.isPlaying) OnBeforeRender();
        }
#endif

        private void OnEnable()
        {
            ScheduleJob();
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= OnBeforeRender;
        }


        private void OnDestroy()
        {
            CleanupJob(true);
            if (_mesh)
            {
                CoreUtils.Destroy(_mesh);
                _mesh = null;
            }
        }

        private void OnValidate()
        {
            _size = math.max(_size, 0);
            CleanupJob(true);
            ScheduleJob();
        }

        private void ScheduleJob()
        {
            Application.onBeforeRender += OnBeforeRender;
            CleanupJob(true);

            var counts = (int2) math.round(_size * _density);
            var verticesCount = counts.x * counts.y;
            var boundsSize = new float3(_size.x, 0, _size.y);
            var job = new MeshGenerationJob
            {
                VertexBuffer = new NativeArray<VertexAttribute>(verticesCount, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory
                ),
                IndexBuffer =
                    new NativeArray<int>(verticesCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory),
                BoundsSize = boundsSize,
                Counts = counts,
                HalfBoundsSize = boundsSize * 0.5f,
            };
            _activeJob = job;
            _jobHandle = job.Schedule(verticesCount, counts.x);
        }

        private void OnBeforeRender()
        {
            if (_jobHandle == null) return;

            _jobHandle.Value.Complete();

            if (!_mesh)
                _mesh = new Mesh();
            _mesh.Clear();
            if (!_meshFilter)
                _meshFilter = GetComponent<MeshFilter>();


            if (_activeJob != null)
            {
                Application.onBeforeRender -= OnBeforeRender;

                var job = _activeJob.Value;
                var verticesCount = job.VertexBuffer.Length;
                _mesh.SetVertexBufferParams(verticesCount, VertexAttributeDescriptors);
                _mesh.SetVertexBufferData(job.VertexBuffer, 0, 0, verticesCount,
                    flags: MeshUpdateFlags.DontRecalculateBounds
                );
                _mesh.SetIndices(job.IndexBuffer, MeshTopology.Points, 0, false);
                _mesh.bounds = new Bounds(Vector3.zero, job.BoundsSize);
                _meshFilter.sharedMesh = _mesh;
            }

            CleanupJob(false);
        }

        private void CleanupJob(bool forceComplete)
        {
            if (_jobHandle != null)
            {
                if (forceComplete)
                    _jobHandle?.Complete();
                _jobHandle = null;
            }

            if (_activeJob != null)
            {
                _activeJob.Value.VertexBuffer.Dispose();
                _activeJob.Value.IndexBuffer.Dispose();
                _activeJob = null;
            }
        }

        [BurstCompile]
        private struct MeshGenerationJob : IJobParallelFor
        {
            [WriteOnly]
            public NativeArray<VertexAttribute> VertexBuffer;
            [WriteOnly]
            public NativeArray<int> IndexBuffer;

            public int2 Counts;
            public float3 BoundsSize;
            public float3 HalfBoundsSize;

            public void Execute(int index)
            {
                var xi = index % Counts.x;
                var yi = index / Counts.x;
                var xt = (float) xi / (Counts.x - 1);
                var yt = (float) yi / (Counts.y - 1);
                VertexBuffer[index] = new VertexAttribute
                {
                    Vertex = math.lerp(0, BoundsSize, new float3(xt, 0, yt)) - HalfBoundsSize,
                    Normal = new float3(0, 1, 0),
                    Tangent = new float4(0, 0, 1, 1),
                };
                IndexBuffer[index] = index;
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