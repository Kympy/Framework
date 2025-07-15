using UnityEngine;

namespace Framework.Extensions
{
    public static class VectorExtensions
    {
        public static bool IsZero(this Vector3 vector)
        {
            return vector.sqrMagnitude < Mathf.Epsilon;
        }
    }
}
