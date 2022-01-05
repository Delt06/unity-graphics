using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
            _materialPropertyBlock ??= new MaterialPropertyBlock();

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var index = 0; index < _chunks.Count; index++)
            {
                var chunk = _chunks[index];
                var chunkMatrix = chunk.GetMatrix();
                var center = chunkMatrix.MultiplyPoint(Vector3.zero);
                var worldBounds = GetWorldBounds(center, ref chunkMatrix, meshBounds);

                if (!GeometryUtility.TestPlanesAABB(_frustumPlanes, worldBounds))
                    continue;

                chunk.Render(_settings, cameraPosition, _offsets, _materialPropertyBlock, UpdatePropertyBlock);
            }
        }

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