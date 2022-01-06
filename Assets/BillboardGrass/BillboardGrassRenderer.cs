using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

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

            var rendererPosition = transform.position;
            var random = new Random(rendererPosition.GetHashCode());
            var instancesCount = 0;

            // ReSharper disable once ForCanBeConvertedToForeach
            var grassChunksCount = _grassChunks.Count;
            for (var index = 0; index < grassChunksCount; index++)
            {
                var grassChunk = _grassChunks[index];

                var origin = grassChunk.GetOrigin();
                var cameraDistance = Vector3.Distance(origin, cam.transform.position);
                var step = _settings.StepOverCameraDistance.Evaluate(cameraDistance);
                step = Mathf.Max(step, 0.05f);

                var size = _settings.Size;
                var scaleRange = _settings.ScaleRange;
                var verticalOffset = _settings.VerticalOffset;
                var maxRandomOffset = _settings.MaxRandomOffset;

                for (var x = 0f; x <= size.x; x += step)
                {
                    for (var y = 0f; y <= size.y; y += step)
                    {
                        var randomAngle = Mathf.Lerp(0f, 360f, (float) random.NextDouble());
                        var extraRotation = Matrix4x4.Rotate(Quaternion.Euler(0f, randomAngle, 0f));
                        var extraScale = Mathf.Lerp(scaleRange.x, scaleRange.y, (float) random.NextDouble());
                        var scale = Matrix4x4.Scale(Vector3.one *
                                                    extraScale
                        );
                        var position = origin + new Vector3(x, verticalOffset * extraScale, y);
                        position += Quaternion.Euler(0f, Mathf.Lerp(0f, 360f, (float) random.NextDouble()), 0f) *
                                    Vector3.forward * ((float) random.NextDouble() * maxRandomOffset);
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

        public void Add(BillboardGrassChunk grassChunk) => _grassChunks.Add(grassChunk);

        public void Remove(BillboardGrassChunk grassChunk) => _grassChunks.Remove(grassChunk);

        private void Render(Matrix4x4 matrix, ref int instancesCount)
        {
            _matrices[instancesCount] = matrix;
            instancesCount++;

            if (instancesCount < MaxInstances) return;

            Flush(ref instancesCount);
        }

        private void Flush(ref int instancesCount)
        {
            if (instancesCount == 0) return;
            Graphics.DrawMeshInstanced(_settings.Mesh, 0, _settings.Material, _matrices, instancesCount);
            instancesCount = 0;
        }
    }
}