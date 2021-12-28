using UnityEngine;

namespace BlobShadows
{
    [ExecuteAlways]
    public class BlobShadowCaster : MonoBehaviour
    {
        [SerializeField] [Range(0.001f, 20f)] private float _power = 2f;

        public float Power => _power;

        private void Awake()
        {
            BlobShadows.ShadowCasters.Add(this);
        }

        private void OnDestroy()
        {
            BlobShadows.ShadowCasters.Remove(this);
        }
    }
}