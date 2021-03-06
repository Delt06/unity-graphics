using Unity.Mathematics;
using UnityEngine;

namespace BillboardGrass
{
    public class BillboardGrassMask : MonoBehaviour
    {
        [SerializeField] private Transform _position;
        [SerializeField] private float _positionSize;
        [SerializeField] private float4 _positionTilingOffset = new float4(1, 1, 0, 0);
        [SerializeField] private int2 _resolution = new int2(1024, 1024);

        [SerializeField]
        private RenderTexture _renderTexture1;
        [SerializeField]
        private RenderTexture _renderTexture2;
        [SerializeField] private Material _stampMaterial;
        [SerializeField] private Material _fadeMaterial;

        private void Awake()
        {
            _renderTexture1 = CreateRt();
            _renderTexture2 = CreateRt();
            Graphics.Blit(Texture2D.blackTexture, _renderTexture1);
        }

        private void Update()
        {
            var position = WorldPositionToNormalized(_position.position);
            TryDraw(position, _positionSize);
            Graphics.Blit(_renderTexture1, _renderTexture2, _fadeMaterial);
            (_renderTexture1, _renderTexture2) = (_renderTexture2, _renderTexture1);

            Shader.SetGlobalTexture("_BillboardGrassMask", _renderTexture1);
            Shader.SetGlobalVector("_BillboardGrassMask_ST", _positionTilingOffset);
        }

        private void OnDestroy()
        {
            DestroyRt(ref _renderTexture1);
            DestroyRt(ref _renderTexture2);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;

            var min = NormalizedPositionToWorld(0);
            var max = NormalizedPositionToWorld(1);
            var size = max - min;
            var center = min + size * 0.5f;
            Gizmos.DrawWireCube(center, size);
        }

        private float2 WorldPositionToNormalized(float3 positionWs)
        {
            var position = positionWs.xz;
            position = position * _positionTilingOffset.xy + _positionTilingOffset.zw;
            return position;
        }

        private float3 NormalizedPositionToWorld(float2 positionNormalized)
        {
            var position = positionNormalized.xxy;
            position.y = 0f;
            position.xz = (position.xz - _positionTilingOffset.zw) / _positionTilingOffset.xy;
            return position;
        }

        private void TryDraw(float2 normalizedPosition, float halfBrushSizeNormalized)
        {
            var activeRt = RenderTexture.active;
            RenderTexture.active = _renderTexture1;
            GL.PushMatrix();

            _stampMaterial.SetPass(0);
            GL.LoadOrtho();
            GL.Begin(GL.QUADS);

            GL.TexCoord2(0, 0);
            GlVertex(normalizedPosition + new float2(-halfBrushSizeNormalized, -halfBrushSizeNormalized));
            GL.TexCoord2(0, 1);
            GlVertex(normalizedPosition + new float2(-halfBrushSizeNormalized, halfBrushSizeNormalized));
            GL.TexCoord2(1, 1);
            GlVertex(normalizedPosition + new float2(halfBrushSizeNormalized, halfBrushSizeNormalized));
            GL.TexCoord2(1, 0);
            GlVertex(normalizedPosition + new float2(halfBrushSizeNormalized, -halfBrushSizeNormalized));

            GL.End();

            GL.PopMatrix();
            RenderTexture.active = activeRt;
        }

        private static void GlVertex(float2 vertex)
        {
            GL.Vertex(new Vector3(vertex.x, vertex.y));
        }

        private RenderTexture CreateRt() =>
            new RenderTexture(_resolution.x, _resolution.y, 0, RenderTextureFormat.R8, 0);

        private static void DestroyRt(ref RenderTexture renderTexture)
        {
            if (renderTexture == null) return;
            Destroy(renderTexture);
            renderTexture = null;
        }
    }
}