using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace BillboardGrass
{
    [ExecuteAlways]
    public class BillboardGrassRenderer : MonoBehaviour
    {
        private const int MaxInstances = 1023;

        [SerializeField] private BillboardGrassRenderingSettings _settings;

        private readonly List<BillboardGrassChunk> _grassChunks = new List<BillboardGrassChunk>();
        private readonly Matrix4x4[] _matrices = new Matrix4x4[MaxInstances];

        private void LateUpdate()
        {
            if (_settings == null) return;

            var cam = Camera.main;
            if (cam == null) return;

            var cameraPosition = cam.transform.position;

            var instancesCount = 0;

            var step = _settings.Step;
            const float minStep = 0.1f;
            step = Mathf.Max(step, minStep);


            // ReSharper disable once ForCanBeConvertedToForeach
            var grassChunksCount = _grassChunks.Count;
            for (var index = 0; index < grassChunksCount; index++)
            {
                var grassChunk = _grassChunks[index];

                var chunkOrigin = grassChunk.GetOrigin();
                var size = _settings.Size;
                var chunkBounds = GetChunkBounds(chunkOrigin);
                var cameraDistance = Vector3.Distance(chunkBounds.ClosestPoint(cameraPosition), cameraPosition);
                var skipProbability = _settings.SkipProbabilityOverCameraDistance.Evaluate(cameraDistance);

                var scaleRange = _settings.ScaleRange;
                var verticalOffset = _settings.VerticalOffset;
                var maxRandomOffset = _settings.MaxRandomOffset;

                for (var x = 0f; x <= size.x; x += step)
                {
                    for (var y = 0f; y <= size.y; y += step)
                    {
                        var position = chunkOrigin + new Vector3(x, 0f, y);
                        var random = CreateRandom(position);
                        if (random.NextFloat() <= skipProbability) continue;

                        var randomAngle = random.NextFloat(0f, 360f);
                        var extraRotation = Matrix4x4.Rotate(Quaternion.Euler(0f, randomAngle, 0f));
                        var extraScale = random.NextFloat(scaleRange.x, scaleRange.y);
                        var scale = Matrix4x4.Scale(Vector3.one * extraScale);
                        position += Quaternion.Euler(0f, random.NextFloat(0f, 360f), 0f) *
                                    Vector3.forward * (random.NextFloat() * maxRandomOffset);
                        position.y += verticalOffset * extraScale;
                        var translate = Matrix4x4.Translate(position);
                        var matrix = translate * extraRotation * scale;
                        var otherMatrix = translate * Matrix4x4.Rotate(Quaternion.Euler(0f, 90f + randomAngle, 0f)) *
                                          scale;
                        Render(matrix, ref instancesCount);
                        Render(otherMatrix, ref instancesCount);
                    }
                }
            }

            Flush(ref instancesCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Bounds GetChunkBounds(in Vector3 chunkOrigin)
        {
            var size = _settings.Size;
            var maxRandomOffsetVector = new Vector3(1, 0, 1) * _settings.MaxRandomOffset;
            var boundsMin = chunkOrigin - maxRandomOffsetVector;
            var maxVerticalSize = _settings.VerticalOffset * _settings.ScaleRange.y * 2f;
            var boundsSize = new Vector3(size.x, maxVerticalSize, size.y) + 2 * maxRandomOffsetVector;
            var boundsCenter = boundsMin + boundsSize * 0.5f;
            var bounds = new Bounds(boundsCenter, boundsSize);
            return bounds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Random CreateRandom(Vector3 position)
        {
            var rendererPosition = position;
            var seed = math.max(1, unchecked((uint) rendererPosition.GetHashCode()));
            var random = new Random(seed);
            return random;
        }

        public void Add(BillboardGrassChunk grassChunk) => _grassChunks.Add(grassChunk);

        public void Remove(BillboardGrassChunk grassChunk) => _grassChunks.Remove(grassChunk);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Render(Matrix4x4 matrix, ref int instancesCount)
        {
            _matrices[instancesCount] = matrix;
            instancesCount++;

            if (instancesCount < MaxInstances) return;

            Flush(ref instancesCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Flush(ref int instancesCount)
        {
            if (instancesCount == 0) return;
            Graphics.DrawMeshInstanced(_settings.Mesh, 0, _settings.Material, _matrices, instancesCount);
            instancesCount = 0;
        }
    }
}