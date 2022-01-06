using UnityEngine;

namespace BillboardGrass
{
    [ExecuteAlways]
    public class BillboardGrassChunk : MonoBehaviour
    {
        [SerializeField] private BillboardGrassRenderer _renderer;

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
                _renderer = GetComponentInParent<BillboardGrassRenderer>();
        }

        public Vector3 GetOrigin() => transform.position;
    }
}