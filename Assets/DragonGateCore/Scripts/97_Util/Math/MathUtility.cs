using UnityEngine;

namespace DragonGate
{
    public class MathHelper
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
        
        public static long RoundToLong(double value)
        {
            return (long)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
        }
        
        public static long RoundToLong(float value)
        {
            return (long)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
        }
        
        public static long CeilToLong(double value)
        {
            return (long)System.Math.Ceiling(value);
        }

        public static long CeilToLong(float value)
        {
            return (long)System.Math.Ceiling(value);
        }
        
        public static long FloorToLong(double value)
        {
            return (long)System.Math.Floor(value);
        }

        public static long FloorToLong(float value)
        {
            return (long)System.Math.Floor(value);
        }
    }
}