using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Grass
{
    [ExecuteAlways]
    public class GrassRenderer : MonoBehaviour
    {
        private const int MaxInstances = 1023;

        private static readonly int OffsetsId = Shader.PropertyToID("_Offsets");
        private static readonly int WindSpeedId = Shader.PropertyToID("_WindSpeed");
        private static readonly int WindMaxDistanceId = Shader.PropertyToID("_WindMaxDistance");
        [SerializeField] private GrassRenderingSettings _settings;

        private readonly List<GrassChunk> _chunks = new List<GrassChunk>();
        private readonly Plane[] _frustumPlanes = new Plane[6];
        private readonly Matrix4x4[] _matrices = new Matrix4x4[MaxInstances];
        private readonly Vector4[] _offsets = new Vector4[MaxInstances];
        private MaterialPropertyBlock _materialPropertyBlock;

        private void LateUpdate()
        {
#if DEBUG
            if (_settings == null) return;

            if (_settings.Material == null)
            {
                Debug.LogWarning("Material is not assigned in the settings.");
                return;
            }

            if (_settings.Mesh == null)
            {
                Debug.LogWarning("Mesh is not assigned in the settings.");
                return;
            }
#endif

            var cam = Camera.main;
            if (cam == null) return;
            GeometryUtility.CalculateFrustumPlanes(cam, _frustumPlanes);

            var meshBounds = _settings.Mesh.bounds;
            var cameraPosition = cam.transform.position;
            var maxOffset = _settings.MaxOffset;
            var uvOffsetFrequency = _settings.UVOffsetFrequency;
            var maxUvOffset = _settings.MaxUvOffset;

            var instancesCount = 0;
            _materialPropertyBlock ??= new MaterialPropertyBlock();

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var index = 0; index < _chunks.Count; index++)
            {
                var chunk = _chunks[index];
                RenderChunk(chunk, meshBounds, cameraPosition, ref instancesCount,
                    maxOffset, uvOffsetFrequency, maxUvOffset
                );
            }

            Flush(instancesCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RenderChunk(GrassChunk chunk, in Bounds meshBounds, in Vector3 cameraPosition,
            ref int instancesCount,
            float maxOffset, float uvOffsetFrequency, float maxUvOffset)
        {
            var chunkMatrix = chunk.GetMatrix();
            var center = chunkMatrix.MultiplyPoint(Vector3.zero);
            var worldBounds = GetWorldBounds(center, ref chunkMatrix, meshBounds);

            if (!GeometryUtility.TestPlanesAABB(_frustumPlanes, worldBounds))
                return;

            var cameraDistance = Vector3.Distance(center, cameraPosition);
            var steps = (int) _settings.StepsOverCameraDistance.Evaluate(cameraDistance);
            if (steps <= 0) return;

            for (var stepIndex = 0; stepIndex < steps; stepIndex++)
            {
                _offsets[instancesCount] =
                    ComputeOffset(stepIndex, steps, maxOffset, uvOffsetFrequency, maxUvOffset);
                _matrices[instancesCount] = chunkMatrix;
                instancesCount++;
                TryFlush(ref instancesCount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector4 ComputeOffset(int stepIndex, int steps, float maxOffset, float uvOffsetFrequency,
            float maxUvOffset)
        {
            var normalizedOffset = (float) stepIndex / (steps - 1);
            var offset = new Vector4
            {
                x = normalizedOffset * maxOffset,
                y = normalizedOffset,
            };

            math.sincos(normalizedOffset * uvOffsetFrequency, out var offsetDirY, out var offsetDirX);
            var uvOffset = new float2(offsetDirX, offsetDirY) * (maxUvOffset * normalizedOffset);
            offset.z = uvOffset.x;
            offset.w = uvOffset.y;
            return offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryFlush(ref int instancesCount)
        {
            if (instancesCount < MaxInstances) return;

            Flush(instancesCount);
            instancesCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Flush(int instancesCount)
        {
            if (instancesCount == 0) return;
            UpdatePropertyBlock();

            const int subMeshIndex = 0;
            Graphics.DrawMeshInstanced(_settings.Mesh, subMeshIndex, _settings.Material,
                _matrices, instancesCount,
                _materialPropertyBlock
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdatePropertyBlock()
        {
            _materialPropertyBlock.SetVectorArray(OffsetsId, _offsets);
            _materialPropertyBlock.SetFloat(WindSpeedId, _settings.WindSpeed);
            _materialPropertyBlock.SetFloat(WindMaxDistanceId, _settings.WindMaxDistance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Bounds GetWorldBounds(in Vector3 center, ref Matrix4x4 chunkMatrix, in Bounds meshBounds)
        {
            var worldBounds = new Bounds(center, Vector3.zero);
            worldBounds.Encapsulate(chunkMatrix.MultiplyPoint(meshBounds.min));
            worldBounds.Encapsulate(chunkMatrix.MultiplyPoint(meshBounds.max));
            return worldBounds;
        }

        public void Add(GrassChunk chunk)
        {
            _chunks.Add(chunk);
        }

        public void Remove(GrassChunk chunk)
        {
            _chunks.Remove(chunk);
        }
    }
}