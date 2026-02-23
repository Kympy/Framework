using UnityEngine;

namespace DragonGate
{
    public class Mathhh
    {
        public static Vector3 BezierPoint(float t, Vector3 start, Vector3 control, Vector3 end)
        {
            float u = 1 - t;
            return (u * u * start) + (2 * u * t * control) + (t * t * end);
        }
        
        public static float BezierFloat(float t, float start, float control, float end)
        {
            float u = 1 - t;
            return (u * u * start) + (2 * u * t * control) + (t * t * end);
        }
    }
}