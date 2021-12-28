using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BlobShadows
{
    public class BlobShadowsRendererPass : ScriptableRenderPass
    {
        private static readonly int ShadowMapId = Shader.PropertyToID("_ShadowMapBlob");
        private static readonly int ShadowMapParamsId = Shader.PropertyToID("_ShadowMapBlobParams");

        private readonly Plane[] _cameraFrustumPlanes = new Plane[6];
        private readonly Vector3[] _corners = new Vector3[4];
        private RenderTexture _rt;
        private Bounds _shadowFrustumAABB;

        public BlobShadowsRendererFeature.Settings Settings { get; set; }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (Settings == null) return;
            var material = Settings.Material;
            if (material == null) return;

            RecalculateBounds(renderingData.cameraData.camera);
            TryDispose();

            var size = new Vector2(_shadowFrustumAABB.size.x, _shadowFrustumAABB.size.z);

            var rtWidth = Mathf.CeilToInt(size.x * Settings.ResolutionPerUnit);
            var rtHeight = Mathf.CeilToInt(size.y * Settings.ResolutionPerUnit);
            if (rtWidth <= 0 || rtHeight <= 0) return;

            _rt = RenderTexture.GetTemporary(rtWidth, rtHeight, 0, RenderTextureFormat.R8,
                RenderTextureReadWrite.Linear
            );
            Graphics.SetRenderTarget(_rt);

            GL.Clear(false, true, Color.black);
            GL.Begin(GL.QUADS);
            GL.PushMatrix();

            var shadowFrustumCenter = _shadowFrustumAABB.center;
            var offsetX = shadowFrustumCenter.x;
            var offsetY = shadowFrustumCenter.z;
            var halfSize = size * 0.5f;
            GL.LoadProjectionMatrix(Matrix4x4.Ortho(
                    -halfSize.x + offsetX,
                    halfSize.x + offsetX,
                    -halfSize.y + offsetY,
                    halfSize.y + offsetY,
                    0f, 1f
                )
            );

            material.SetPass(0);


            var extraShadowScaling = Settings.ExtraShadowScaling;

            foreach (var planarShadowCaster in BlobShadows.ShadowCasters)
            {
                var t = planarShadowCaster.transform;

                var position = t.position;
                float x = position.x, y = position.z;
                var lossyScale = t.lossyScale;
                var sizeX = lossyScale.x;
                var sizeY = lossyScale.z;
                var halfSizeX = sizeX * 0.5f * extraShadowScaling;
                var halfSizeY = sizeY * 0.5f * extraShadowScaling;
                var shadowBounds = new Bounds(new Vector3(x, shadowFrustumCenter.y, y),
                    new Vector3(halfSizeX, _shadowFrustumAABB.size.y, halfSizeY)
                );
                if (!GeometryUtility.TestPlanesAABB(_cameraFrustumPlanes, shadowBounds)) continue;

                GL.Color(new Color(planarShadowCaster.Power, 0f, 0f));
                GL.TexCoord2(0f, 0f);
                GL.Vertex3(x - halfSizeX, y - halfSizeY, 0);
                GL.TexCoord2(0f, 1f);
                GL.Vertex3(x - halfSizeX, y + halfSizeY, 0);
                GL.TexCoord2(1f, 1f);
                GL.Vertex3(x + halfSizeX, y + halfSizeY, 0);
                GL.TexCoord2(1f, 0f);
                GL.Vertex3(x + halfSizeX, y - halfSizeY, 0);
            }

            GL.End();
            GL.PopMatrix();

            Graphics.SetRenderTarget(null);

            Shader.SetGlobalTexture(ShadowMapId, _rt);
            Shader.SetGlobalVector(ShadowMapParamsId, new Vector4(size.x, size.y, offsetX, offsetY));
        }

        private void RecalculateBounds(Camera camera)
        {
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;

            camera.CalculateFrustumCorners(camera.rect, Mathf.Min(Settings.ShadowDistance, camera.farClipPlane),
                Camera.MonoOrStereoscopicEye.Mono, _corners
            );
            AddToMinMax(ref min, ref max, camera.transform, _corners);

            camera.CalculateFrustumCorners(camera.rect, camera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono,
                _corners
            );
            AddToMinMax(ref min, ref max, camera.transform, _corners);
            _shadowFrustumAABB =
                new Bounds((max + min) * 0.5f, max - min + Vector3.one * Settings.ShadowFrustumPadding);

            GeometryUtility.CalculateFrustumPlanes(camera, _cameraFrustumPlanes);
        }

        private static void AddToMinMax(ref Vector3 min, ref Vector3 max, Transform camTransform, Vector3[] corners)
        {
            foreach (var corner in corners)
            {
                var worldCorner = camTransform.TransformPoint(corner);
                min = math.min(min, worldCorner);
                max = math.max(max, worldCorner);
            }
        }

        private void TryDispose()
        {
            if (!_rt) return;
            RenderTexture.ReleaseTemporary(_rt);
            _rt = null;
        }

        public void Dispose()
        {
            TryDispose();
        }
    }
}