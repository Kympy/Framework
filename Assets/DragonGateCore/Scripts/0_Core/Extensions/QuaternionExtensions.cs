using UnityEngine;

namespace DragonGate
{
    public static class QuaternionExtensions
    {
        public static Quaternion Snap(this Quaternion quaternion, float stepDegrees)
        {
            Vector3 eulerAngles = quaternion.eulerAngles;

            eulerAngles.x = Mathf.Round(eulerAngles.x / stepDegrees) * stepDegrees;
            eulerAngles.y = Mathf.Round(eulerAngles.y / stepDegrees) * stepDegrees;
            eulerAngles.z = Mathf.Round(eulerAngles.z / stepDegrees) * stepDegrees;

            return Quaternion.Euler(eulerAngles);
        }
        
        public static Quaternion SnapY(Quaternion quaternion, float stepDegrees)
        {
            Vector3 eulerAngles = quaternion.eulerAngles;

            eulerAngles.y = Mathf.Round(eulerAngles.y / stepDegrees) * stepDegrees;

            return Quaternion.Euler(0f, eulerAngles.y, 0f);
        }
    }
}
