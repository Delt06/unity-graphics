using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlobShadows
{
    public class BlobShadowCaster : MonoBehaviour
    {
        public enum SdfType
        {
            Circle,
            Box,
        }

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

        public static class SdfTypes
        {
            public static readonly IReadOnlyList<SdfType> All = (SdfType[]) Enum.GetValues(typeof(SdfType));
        }
    }
}