using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Grass
{
    [ExecuteAlways]
    public class GrassRenderer : MonoBehaviour
    {
        private const int MaxInstances = 1023;
        private static readonly int OffsetsId = Shader.PropertyToID("_Offsets");

        [SerializeField] private GrassRenderingSettings _settings;

        private readonly Plane[] _frustumPlanes = new Plane[6];
        private readonly List<Matrix4x4> _matrices = new List<Matrix4x4>();
        private readonly Vector4[] _offsets = new Vector4[MaxInstances];
        private MaterialPropertyBlock _materialPropertyBlock;

        private void LateUpdate()
        {
#if DEBUG
            if (_settings == null) return;

            var material = _settings.Material;
            if (material == null) return;

            var mesh = _settings.Mesh;
            if (mesh == null) return;
#endif

            var cam = Camera.main;
            if (cam == null) return;

            var matrix = transform.localToWorldMatrix;
            var center = matrix.MultiplyPoint(Vector3.zero);
            GeometryUtility.CalculateFrustumPlanes(cam, _frustumPlanes);

            var worldBounds = new Bounds(center, Vector3.zero);
            var meshBounds = mesh.bounds;
            worldBounds.Encapsulate(matrix.MultiplyPoint(meshBounds.min));
            worldBounds.Encapsulate(matrix.MultiplyPoint(meshBounds.max));

            if (!GeometryUtility.TestPlanesAABB(_frustumPlanes, worldBounds))
                return;

            var cameraDistance = Vector3.Distance(center, cam.transform.position);
            var steps = (int) _settings.StepsOverCameraDistance.Evaluate(cameraDistance);
            if (steps <= 0) return;

            _matrices.Clear();
            var maxOffset = _settings.MaxOffset;
            var uvOffsetFrequency = _settings.UVOffsetFrequency;
            var maxUvOffset = _settings.MaxUvOffset;

            for (var i = 0; i < steps; i++)
            {
                _matrices.Add(matrix);
                var normalizedOffset = (float) i / (steps - 1);
                var offset = new Vector4
                {
                    x = normalizedOffset * maxOffset,
                    y = normalizedOffset,
                };

                math.sincos(normalizedOffset * uvOffsetFrequency, out var sin, out var cos);
                var uvOffset = new float2(sin, cos) * (maxUvOffset * normalizedOffset);
                offset.z = uvOffset.x;
                offset.w = uvOffset.y;

                _offsets[i] = offset;
            }

            _materialPropertyBlock ??= new MaterialPropertyBlock();
            _materialPropertyBlock.SetVectorArray(OffsetsId, _offsets);
            Graphics.DrawMeshInstanced(mesh, 0, material, _matrices, _materialPropertyBlock);
        }
    }
}