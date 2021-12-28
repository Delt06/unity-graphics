using UnityEngine;

namespace BlobShadows
{
    [ExecuteAlways]
    public class BlobShadowCaster : MonoBehaviour
    {
        [SerializeField] private SdfType _type = SdfType.Circle;

        public SdfType Type => _type;

        private void Awake()
        {
            BlobShadows.ShadowCasters.Add(this);
        }

        private void OnDestroy()
        {
            BlobShadows.ShadowCasters.Remove(this);
        }

        public enum SdfType
        {
            Circle, Box,
        }
    }
}