using System.Collections.Generic;
using UnityEngine;

namespace BlobShadows
{
    public static class BlobShadows
    {
        public static readonly List<BlobShadowCaster> ShadowCasters = new List<BlobShadowCaster>();


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            ShadowCasters.Clear();
        }
    }
}