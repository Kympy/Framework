using UnityEngine;

namespace DragonGate
{
    public static class VectorExtensions
    {
        public static bool IsZero(this Vector3 vector)
        {
            return Mathf.Approximately(vector.sqrMagnitude, 0);
        }
        
        public static Vector2 Rotate(this Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(
                v.x * cos - v.y * sin,
                v.x * sin + v.y * cos
            );
        }
        
        public static Vector3 Rotate(this Vector3 v, float degrees, Vector3 axis)
        {
            return Quaternion.AngleAxis(degrees, axis.normalized) * v;
        }

        // 대상이 나를 향하는 벡터
        public static Vector3 GetForwardFrom(this Vector3 self, Vector3 from)
        {
            var direction = self - from;
            direction.y = 0;
            direction.Normalize();
            return direction;
        }

        // 내가 대상을 향하는 벡터
        public static Vector3 GetForwardTo(this Vector3 self, Vector3 to)
        {
            var direction = to - self;
            direction.y = 0;
            direction.Normalize();
            return direction;
        }

        public static Vector3 ToInt(this Vector3 self)
        {
            return new Vector3(Mathf.RoundToInt(self.x), Mathf.RoundToInt(self.y), Mathf.RoundToInt(self.z)); 
        }
        
        public static Vector3 Snap(this Vector3 value, float gridSize)
        {
            return new Vector3(
                Mathf.Round(value.x / gridSize) * gridSize,
                Mathf.Round(value.y / gridSize) * gridSize,
                Mathf.Round(value.z / gridSize) * gridSize
            );
        }
        
        public static Vector3 SnapXZ(this Vector3 value, float gridSize)
        {
            return new Vector3(
                Mathf.Round(value.x / gridSize) * gridSize,
                value.y,
                Mathf.Round(value.z / gridSize) * gridSize
            );
        }

        public static Vector3 GetRandomCirclePoint(this Vector3 self, float radius = 1f)
        {
            var randomValue = Random.insideUnitCircle * radius;
            return self + new Vector3(randomValue.x, 0, randomValue.y);
        }
    }
}
