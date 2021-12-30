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

        private Material _material;
        private BlobShadowsRendererPass _pass;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (Application.isPlaying)
                Destroy(_material);
            else
                DestroyImmediate(_material);
            _material = null;
            _pass = null;
        }


        public override void Create()
        {
            _settings ??= new Settings();
            if (!_material)
                _material = new Material(Shader.Find("Hidden/Blob Shadows/Caster"))
                {
                    enableInstancing = SystemInfo.supportsInstancing,
                };
            _material.SetFloat(ThresholdId, _settings.ShadowThreshold);
            _material.SetFloat(SmoothnessId, _settings.ShadowSmoothness);
            _settings.Material = _material;
            _pass = new BlobShadowsRendererPass
            {
                renderPassEvent = RenderPassEvent.BeforeRendering,
                Settings = _settings,
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
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

            public Material Material { get; set; }
        }
    }
}