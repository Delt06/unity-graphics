using UnityEngine;

namespace Grass
{
    [ExecuteAlways]
    public class GrassChunk : MonoBehaviour
    {
        [SerializeField] private GrassRenderer _renderer;

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
                _renderer = GetComponentInParent<GrassRenderer>();
        }

        public Matrix4x4 GetMatrix() => transform.localToWorldMatrix;
    }
}