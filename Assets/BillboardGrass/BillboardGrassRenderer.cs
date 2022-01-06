using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace BillboardGrass
{
    public class BillboardGrassRenderer : MonoBehaviour
    {
        private const int MaxInstances = 1023;

        [SerializeField] private BillboardGrassRenderingSettings _settings;
        private readonly Plane[] _frustumPlanes = new Plane[6];

        private readonly List<BillboardGrassChunk> _grassChunks = new List<BillboardGrassChunk>();
        private readonly Matrix4x4[] _matrices = new Matrix4x4[MaxInstances];
        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;
        }

        private void LateUpdate()
        {
#if DEBUG
            if (_settings == null)
            {
                Debug.LogWarning("Settings are not assigned.");
                return;
            }
#endif

            var cameraPosition = _camera.transform.position;
            GeometryUtility.CalculateFrustumPlanes(_camera, _frustumPlanes);

            var instancesCount = 0;
            var lods = _settings.Lods;


            // ReSharper disable once ForCanBeConvertedToForeach
            var grassChunksCount = _grassChunks.Count;
            for (var index = 0; index < grassChunksCount; index++)
            {
                var grassChunk = _grassChunks[index];
                var chunkOrigin = grassChunk.GetOrigin();
                var chunkBounds = GetChunkBounds(chunkOrigin);
                if (!GeometryUtility.TestPlanesAABB(_frustumPlanes, chunkBounds)) continue;

                var cameraDistance = Vector3.Distance(chunkBounds.ClosestPoint(cameraPosition), cameraPosition);

                var lodIndex = GetLodIndex(cameraDistance, lods);
                var matricesLod = grassChunk.GetMatricesLod(lodIndex);
                var matricesCount = matricesLod.Length;
                for (var i = 0; i < matricesCount; i++)
                {
                    var matrix = matricesLod[i];
                    Render(matrix, ref instancesCount);
                }
            }

            Flush(ref instancesCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetLodIndex(float cameraDistance, BillboardGrassRenderingSettings.Lod[] lods)
        {
            for (var lodIndex = lods.Length - 1; lodIndex >= 0; lodIndex--)
            {
                var lod = lods[lodIndex];
                if (cameraDistance >= lod.Distance)
                    return lodIndex;
            }

            return 0;
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

        public void Add(BillboardGrassChunk grassChunk)
        {
            _grassChunks.Add(grassChunk);
            grassChunk.Generate(_settings);
        }

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