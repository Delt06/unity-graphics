using UnityEngine;

namespace BillboardGrass
{
    [CreateAssetMenu(menuName = "DELTation/Billboard Grass Rendering Settings")]
    public class BillboardGrassRenderingSettings : ScriptableObject
    {
        [SerializeField] private Material _material;
        [SerializeField] private Mesh _mesh;
        [SerializeField] private Vector2 _size;
        [SerializeField] [Min(0f)] private float _maxRandomOffset = 0.15f;
        [SerializeField] private Vector2 _scaleRange = new Vector2(0.75f, 1.25f);
        [SerializeField] [Min(0f)] private float _verticalOffset = 0.5f;
        [SerializeField] [Min(0f)] private float _step = 0.5f;
        [SerializeField] private AnimationCurve _skipProbabilityOverCameraDistance;

        public Material Material => _material;

        public Mesh Mesh => _mesh;

        public Vector2 Size => _size;


        public float MaxRandomOffset => _maxRandomOffset;

        public Vector2 ScaleRange => _scaleRange;

        public float VerticalOffset => _verticalOffset;

        public float Step => _step;

        public AnimationCurve SkipProbabilityOverCameraDistance => _skipProbabilityOverCameraDistance;
    }
}