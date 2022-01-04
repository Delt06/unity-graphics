using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static BlobShadows.BlobShadowsRendererFeature.Settings;

namespace BlobShadows
{
    public class BlobShadowsRendererPass : ScriptableRenderPass
    {
        private const int MaxInstances = 1023;
        private const string ShadowMapName = "_ShadowMapBlob";
        private static readonly int ShadowMapParamsId = Shader.PropertyToID("_ShadowMapBlobParams");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int BlendOpId = Shader.PropertyToID("_BlendOp");

        private readonly Plane[] _cameraFrustumPlanes = new Plane[6];
        private readonly Vector3[] _corners = new Vector3[4];
        private readonly Matrix4x4[] _matrices = new Matrix4x4[MaxInstances];
        private readonly Mesh _quadMesh;
        private readonly List<Matrix4x4>[] _shadowCastersByType;
        private int _rtHeight;
        private int _rtWidth;
        private Bounds _shadowFrustumAabb;
        private Vector2 _shadowFrustumAabbSize;

        private RenderTargetHandle _shadowMapHandle;

        public BlobShadowsRendererPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingShadows;

            //  3---2
            //  |   |
            //  0---1

            _quadMesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(-1f, -1f),
                    new Vector3(1f, -1f),
                    new Vector3(1f, 1f),
                    new Vector3(-1f, 1),
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f),
                },
                triangles = new[]
                {
                    // clockwise, to look at camera
                    0, 3, 2,
                    0, 2, 1,
                },
            };

            _shadowCastersByType = new List<Matrix4x4>[BlobShadowCaster.SdfTypes.All.Count];

            for (var i = 0; i < _shadowCastersByType.Length; i++)
            {
                _shadowCastersByType[i] = new List<Matrix4x4>();
            }

            _shadowMapHandle.Init(ShadowMapName);
        }

        public BlobShadowsRendererFeature.Settings Settings { get; set; }

        public Material Material { get; set; }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);

            RecalculateBounds(renderingData.cameraData.camera);
            _shadowFrustumAabbSize = new Vector2(_shadowFrustumAabb.size.x, _shadowFrustumAabb.size.z);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_shadowMapHandle.id);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            _rtWidth = Mathf.CeilToInt(_shadowFrustumAabbSize.x * Settings.ResolutionPerUnit);
            _rtHeight = Mathf.CeilToInt(_shadowFrustumAabbSize.y * Settings.ResolutionPerUnit);
            const RenderTextureFormat format = RenderTextureFormat.R8;
            var desc = new RenderTextureDescriptor(_rtWidth, _rtHeight, format, 0, 0);

            cmd.GetTemporaryRT(_shadowMapHandle.id, desc, Settings.FilterMode);
            cmd.SetRenderTarget(_shadowMapHandle.Identifier(),
                RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.DontCare
            );
            cmd.ClearRenderTarget(false, true, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (Settings == null)
                return;

            var material = Material;
            if (material == null)
                return;

            RecalculateBounds(renderingData.cameraData.camera);

            var cmd = CommandBufferPool.Get(nameof(BlobShadowsRendererPass));
            cmd.Clear();

            var shadowFrustumCenter = _shadowFrustumAabb.center;
            var offsetX = shadowFrustumCenter.x;
            var offsetY = shadowFrustumCenter.z;
            var halfSize = _shadowFrustumAabbSize * 0.5f;
            var view = Matrix4x4.Translate(new Vector3(-offsetX, -offsetY, -1f));
            var proj = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                new Vector3(1f / halfSize.x, 1f / halfSize.y, 1f)
            );

            cmd.SetViewport(new Rect(0f, 0f, _rtWidth, _rtHeight));
            cmd.SetViewProjectionMatrices(view, proj);

            CullShadowCasters(shadowFrustumCenter);
            SetupBlending(material);
            RenderShadowCasters(cmd, material);

            ClearShadowCasters();

            cmd.SetGlobalTexture(ShadowMapName, _shadowMapHandle.Identifier());
            cmd.SetGlobalVector(ShadowMapParamsId,
                new Vector4(_shadowFrustumAabbSize.x, _shadowFrustumAabbSize.y, offsetX, offsetY)
            );

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();


            CommandBufferPool.Release(cmd);
        }

        private void CullShadowCasters(Vector3 shadowFrustumCenter)
        {
            var extraShadowScaling = Settings.ExtraShadowScaling;

            ClearShadowCasters();

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var index = 0; index < BlobShadows.ShadowCasters.Count; index++)
            {
                var shadowCaster = BlobShadows.ShadowCasters[index];
                var t = shadowCaster.transform;

                var position = t.position;
                float x = position.x, y = position.z;
                var lossyScale = t.lossyScale;
                var sizeX = lossyScale.x;
                var sizeY = lossyScale.z;
                var halfSizeX = sizeX * 0.5f * extraShadowScaling;
                var halfSizeY = sizeY * 0.5f * extraShadowScaling;
                var shadowBounds = new Bounds(new Vector3(x, shadowFrustumCenter.y, y),
                    new Vector3(halfSizeX, _shadowFrustumAabb.size.y, halfSizeY)
                );
                if (!GeometryUtility.TestPlanesAABB(_cameraFrustumPlanes, shadowBounds)) continue;

                var rotationEuler = t.eulerAngles;
                rotationEuler.z = -rotationEuler.y;
                rotationEuler.x = 0f;
                rotationEuler.y = 0f;

                var matrix = Matrix4x4.TRS(
                    new Vector3(x, y),
                    Quaternion.Euler(rotationEuler),
                    new Vector3(halfSizeX, halfSizeY, 1f)
                );

                var list = _shadowCastersByType[(int) shadowCaster.Type];
                list.Add(matrix);
            }
        }

        private void ClearShadowCasters()
        {
            foreach (var list in _shadowCastersByType)
            {
                list.Clear();
            }
        }

        private void SetupBlending(Material material)
        {
            var (srcBlend, dstBlend, blendOp) = Settings.BlendingMode switch
            {
                BlobBlendingMode.MetaBalls => (BlendMode.SrcColor, BlendMode.One, BlendOp.Add),
                BlobBlendingMode.Voronoi => (BlendMode.One, BlendMode.One, BlendOp.Max),
                _ => throw new ArgumentOutOfRangeException(),
            };
            material.SetFloat(SrcBlendId, (float) srcBlend);
            material.SetFloat(DstBlendId, (float) dstBlend);
            material.SetFloat(BlendOpId, (float) blendOp);
        }

        private void RenderShadowCasters(CommandBuffer cmd, Material material)
        {
            for (var i = 0; i < _shadowCastersByType.Length; i++)
            {
                var sdfType = (BlobShadowCaster.SdfType) i;
                var shadowCasters = _shadowCastersByType[i];
                if (shadowCasters.Count == 0) continue;

                var keyword = sdfType switch
                {
                    BlobShadowCaster.SdfType.Circle => "SDF_CIRCLE",
                    BlobShadowCaster.SdfType.Box => "SDF_BOX",
                    _ => throw new ArgumentOutOfRangeException(),
                };
                cmd.EnableShaderKeyword(keyword);


                var instances = 0;

                for (var index = 0; index < shadowCasters.Count; index++)
                {
                    var matrix = shadowCasters[index];
                    _matrices[index] = matrix;
                    instances++;
                    if (instances < MaxInstances) continue;
                    DrawQuads(cmd, material, instances);
                    instances = 0;
                }

                if (instances > 0)
                    DrawQuads(cmd, material, instances);

                cmd.DisableShaderKeyword(keyword);
            }
        }

        private void DrawQuads(CommandBuffer cmd, Material material, int count)
        {
            Assert.IsTrue(material.enableInstancing, "Instancing is not enabled in the material.");

            const int subMeshIndex = 0;
            const int shaderPass = 0;

            if (SystemInfo.supportsInstancing)
                cmd.DrawMeshInstanced(_quadMesh, subMeshIndex, material, shaderPass, _matrices, count);
            else
                for (var i = 0; i < count; i++)
                {
                    var matrix = _matrices[i];
                    cmd.DrawMesh(_quadMesh, matrix, material, subMeshIndex, shaderPass);
                }
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
            _shadowFrustumAabb =
                new Bounds((max + min) * 0.5f, max - min + Vector3.one * Settings.ShadowFrustumPadding);

            GeometryUtility.CalculateFrustumPlanes(camera, _cameraFrustumPlanes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddToMinMax(ref Vector3 min, ref Vector3 max, Transform camTransform, Vector3[] corners)
        {
            for (int index = 0, cornersLength = corners.Length; index < cornersLength; index++)
            {
                var corner = corners[index];
                var worldCorner = camTransform.TransformPoint(corner);
                min = Min(min, worldCorner);
                max = Max(max, worldCorner);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 Min(Vector3 v1, Vector3 v2) =>
            new Vector3(
                math.min(v1.x, v2.x),
                math.min(v1.y, v2.y),
                math.min(v1.z, v2.z)
            );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 Max(Vector3 v1, Vector3 v2) =>
            new Vector3(
                math.max(v1.x, v2.x),
                math.max(v1.y, v2.y),
                math.max(v1.z, v2.z)
            );
    }
}