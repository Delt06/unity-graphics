using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BlobShadows
{
    public class BlobShadowsRendererFeature : ScriptableRendererFeature
    {
        private static readonly int ThresholdId = Shader.PropertyToID("_Threshold");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        [SerializeField] private Settings _settings;

        private Material _material;
        private BlobShadowsRendererPass _pass;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            CoreUtils.Destroy(_material);
            _material = null;
            _pass = null;
        }


        public override void Create()
        {
            _material = new Material(Shader.Find("Hidden/Blob Shadows/Caster"))
            {
                enableInstancing = SystemInfo.supportsInstancing,
            };
            _material.SetFloat(ThresholdId, _settings.ShadowThreshold);
            _material.SetFloat(SmoothnessId, _settings.ShadowSmoothness);
            _pass = new BlobShadowsRendererPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            _pass.Settings = _settings;
            _pass.Material = _material;
            renderer.EnqueuePass(_pass);
        }

        [Serializable]
        public class Settings
        {
            public enum BlobBlendingMode
            {
                MetaBalls,
                Voronoi,
            }

            [Min(0f)] public float ShadowDistance = 15f;
            [Min(0f)] public float ExtraShadowScaling = 2.5f;
            [Min(1)] public float ResolutionPerUnit = 16;
            [Min(0f)] public float ShadowFrustumPadding = 1f;
            [Range(-0.5f, 1f)] public float ShadowThreshold = 0.2f;
            [Range(0.001f, 2f)] public float ShadowSmoothness = 0.1f;
            public BlobBlendingMode BlendingMode = BlobBlendingMode.MetaBalls;
            public FilterMode FilterMode = FilterMode.Bilinear;
        }
    }
}