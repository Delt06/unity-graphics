using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace BillboardGrass
{
    public class BillboardGrassChunk : MonoBehaviour
    {
        [SerializeField] private BillboardGrassRenderer _renderer;

        private bool _isGenerated;
        private Matrix4x4[][] _matrices;

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
                _renderer = GetComponentInParent<BillboardGrassRenderer>();
        }

        public void Generate(BillboardGrassRenderingSettings settings)
        {
            if (_isGenerated) return;
            _isGenerated = true;

            var lods = settings.Lods;
            var lodsCount = lods.Length;
            var matrices = new List<Matrix4x4>[lodsCount];

            for (var i = 0; i < lodsCount; i++)
            {
                matrices[i] = new List<Matrix4x4>();
            }

            var chunkOrigin = GetOrigin();

            var size = settings.Size;
            var scaleRange = settings.ScaleRange;
            var verticalOffset = settings.VerticalOffset;
            var maxRandomOffset = settings.MaxRandomOffset;

            var step = settings.Step;
            const float minStep = 0.1f;
            step = Mathf.Max(step, minStep);

            for (var x = 0f; x <= size.x; x += step)
            {
                for (var y = 0f; y <= size.y; y += step)
                {
                    var position = chunkOrigin + new Vector3(x, 0f, y);
                    var random = BillboardGrassUtils.CreateRandom(position);

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

                    AddMatrix(matrix, ref random, lods, matrices);
                    AddMatrix(otherMatrix, ref random, lods, matrices);
                }
            }

            _matrices = new Matrix4x4[matrices.Length][];

            for (var i = 0; i < _matrices.Length; i++)
            {
                _matrices[i] = matrices[i].ToArray();
            }
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddMatrix(in Matrix4x4 matrix, ref Random random,
            BillboardGrassRenderingSettings.Lod[] lods, List<Matrix4x4>[] matrices)
        {
            for (var lodIndex = 0; lodIndex < lods.Length; lodIndex++)
            {
                var density = lods[lodIndex].Density;
                if (random.NextFloat() >= density) continue;

                matrices[lodIndex].Add(matrix);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4x4[] GetMatricesLod(int lodIndex) => _matrices[lodIndex];

        public Vector3 GetOrigin() => transform.position;
    }
}