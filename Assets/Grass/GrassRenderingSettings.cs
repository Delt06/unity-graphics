using UnityEngine;

namespace Grass
{
    [CreateAssetMenu(menuName = "DELTation/Grass Rendering Setttings")]
    public class GrassRenderingSettings : ScriptableObject
    {
        [SerializeField] private Material _material;
        [SerializeField] private Mesh _mesh;
        [SerializeField] [Min(0f)] private float _maxOffset = 0.1f;
        [SerializeField] private AnimationCurve _stepsOverCameraDistance;
        [SerializeField] [Range(0f, 0.1f)] private float _maxUvOffset = 0.1f;
        [SerializeField] private float _uvOffsetFrequency = 1f;
        [SerializeField] [Min(0f)] private float _windSpeed = 1f;
        [SerializeField] [Min(0f)] private float _windMaxDistance = 25f;

        public Material Material => _material;

        public Mesh Mesh => _mesh;

        public float MaxOffset => _maxOffset;

        public AnimationCurve StepsOverCameraDistance => _stepsOverCameraDistance;

        public float MaxUvOffset => _maxUvOffset;

        public float UVOffsetFrequency => _uvOffsetFrequency;

        public float WindSpeed => _windSpeed;

        public float WindMaxDistance => _windMaxDistance;
    }
}