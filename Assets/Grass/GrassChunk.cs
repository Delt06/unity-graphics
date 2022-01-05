using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Grass
{
    [ExecuteAlways]
    public class GrassChunk : MonoBehaviour
    {
        private const int MaxInstances = 1023;

        [SerializeField] private GrassRenderer _renderer;
        
        private readonly List<Matrix4x4> _matrices = new List<Matrix4x4>();

        private void OnEnable()
        {
            if (_renderer)
                _renderer.Add(this);
        }

        private void OnDisable()
        {
            if (_renderer)
                _renderer.Remove(this);
        }

        private void OnValidate()
        {
            if (!_renderer)
                _renderer = GetComponentInParent<GrassRenderer>();
        }

        public void Render(GrassRenderingSettings settings, Vector3 cameraPosition, Vector4[] offsets, MaterialPropertyBlock materialPropertyBlock, Action updatePropertyBlock)
        {
            var matrix = GetMatrix();
            var center = matrix.MultiplyPoint(Vector3.zero);

            var cameraDistance = Vector3.Distance(center, cameraPosition);
            var steps = (int) settings.StepsOverCameraDistance.Evaluate(cameraDistance);
            if (steps <= 0) return;

            _matrices.Clear();
            var maxOffset = settings.MaxOffset;
            var uvOffsetFrequency = settings.UVOffsetFrequency;
            var maxUvOffset = settings.MaxUvOffset;

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

                offsets[i] = offset;
            }


            updatePropertyBlock();
            Graphics.DrawMeshInstanced(settings.Mesh, 0, settings.Material, _matrices, materialPropertyBlock);
        }

        public Matrix4x4 GetMatrix() => transform.localToWorldMatrix;
    }
}