using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace BillboardGrass
{
    public static class BillboardGrassUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Random CreateRandom(Vector3 position)
        {
            var rendererPosition = position;
            var seed = math.max(1, unchecked((uint) rendererPosition.GetHashCode()));
            var random = new Random(seed);
            return random;
        }
    }
}