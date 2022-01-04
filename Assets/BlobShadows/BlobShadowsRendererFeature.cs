using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BlobShadows
{
    public class BlobShadowsRendererFeature : ScriptableRendererFeature
    {
        private static readonly int ThresholdId = Shader.PropertyToID("_Threshold");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        [SerializeField] private Settings _settings;

        private BlobShadowsRendererPass _pass;


        public override void Create()
        {
            _pass = new BlobShadowsRendererPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            _pass.Settings = _settings;

            if (_settings.Material)
            {
                _settings.Material.SetFloat(ThresholdId, _settings.ShadowThreshold);
                _settings.Material.SetFloat(SmoothnessId, _settings.ShadowSmoothness);
            }

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

            public Material Material;
        }
    }
}