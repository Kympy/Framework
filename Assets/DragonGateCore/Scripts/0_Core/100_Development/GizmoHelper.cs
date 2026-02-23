using UnityEngine;

namespace DragonGate
{
    public class GizmoHelper
    {
        public enum DrawMode
        {
            XY,
            XZ,
        }
        public static void DrawCircle(
            Vector3 center,
            float radius,
            int segments = 12, DrawMode drawMode = DrawMode.XZ,
            Color color = default)
        {
            if (color == default)
            {
                color = Color.red;
            }

            UnityEngine.Gizmos.color = color;

            float angleStep = Mathf.Deg2Rad * (360f / segments);
            float cos = Mathf.Cos(angleStep);
            float sin = Mathf.Sin(angleStep);

            Vector3 direction = new Vector3(1f, 0f, 0f);
            Vector3 previousPoint = center + direction * radius;

            for (int i = 0; i < segments; i++)
            {
                switch (drawMode)
                {
                    case DrawMode.XY:
                        direction = new Vector3(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos, 0f);
                        break;
                    case DrawMode.XZ:
                        direction = new Vector3(direction.x * cos - direction.z * sin, 0f, direction.x * sin + direction.z * cos);
                        break;
                }

                Vector3 nextPoint = center + direction * radius;
                UnityEngine.Gizmos.DrawLine(previousPoint, nextPoint);
                previousPoint = nextPoint;
            }
        }
    }
}
