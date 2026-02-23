using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DragonGate
{
    public class DGDebug
    {
        public const string DebugDefine = "DEBUG_BUILD";

        [Conditional(DebugDefine)]
        public static void Log(string log, Color color = default)
        {
            log = $"[{Time.frameCount}] {log}";
            if (color != default)
                UnityEngine.Debug.Log(log.SetColor(color));
            else
                UnityEngine.Debug.Log(log);
        }

        [Conditional(DebugDefine)]
        public static void Log(object log, Color color = default)
        {
            log = $"[{Time.frameCount}] {log}";
            if (color != default)
                UnityEngine.Debug.Log(log.ToString().SetColor(color));
            else
                UnityEngine.Debug.Log(log);
        }

        [Conditional(DebugDefine)]
        public static void Log<T>(string log, Color color = default) where T : class
        {
            log = $"[{Time.frameCount}] {log}";
            if (color != default)
            {
                string fullLog = $"[{typeof(T)}] : {log}";
                UnityEngine.Debug.Log(fullLog.SetColor(color));
            }
            else
                UnityEngine.Debug.Log($"[{typeof(T)}] : {log}");
        }

        [Conditional(DebugDefine)]
        public static void Log(System.Type type, string log, Color color = default)
        {
            log = $"[{Time.frameCount}] {log}";
            if (color != default)
            {
                string fullLog = $"[{type.Name}] : {log}";
                UnityEngine.Debug.Log(fullLog.SetColor(color));
            }
            else
                UnityEngine.Debug.Log($"[{type.Name}] : {log}");
        }

        [Conditional(DebugDefine)]
        public static void LogWarning(string log)
        {
            UnityEngine.Debug.LogWarning(log);
        }

        [Conditional(DebugDefine)]
        public static void LogWarning(object log)
        {
            UnityEngine.Debug.LogWarning(log);
        }

        [Conditional(DebugDefine)]
        public static void LogWarning<T>(string log) where T : class
        {
            UnityEngine.Debug.LogWarning($"[{typeof(T)}] : {log}");
        }

        [Conditional(DebugDefine)]
        public static void LogError(string log)
        {
            log = $"[{Time.frameCount}] {log}";
            UnityEngine.Debug.LogError(log);
        }

        [Conditional(DebugDefine)]
        public static void LogError(object log)
        {
            log = $"[{Time.frameCount}] {log}";
            UnityEngine.Debug.LogError(log);
        }

        [Conditional(DebugDefine)]
        public static void LogError<T>(string log) where T : class
        {
            UnityEngine.Debug.LogError($"[{typeof(T)}] : {log}");
        }

        [Conditional(DebugDefine)]
        public static void DrawLine(Vector3 start, Vector3 end)
        {
            UnityEngine.Debug.DrawLine(start, end);
        }

        [Conditional(DebugDefine)]
        public static void DrawLine(Vector3 start, Vector3 end, Color color)
        {
            UnityEngine.Debug.DrawLine(start, end, color);
        }

        [Conditional(DebugDefine)]
        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration)
        {
            UnityEngine.Debug.DrawLine(start, end, color, duration);
        }

        [Conditional(DebugDefine)]
        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration, bool depthTest)
        {
            UnityEngine.Debug.DrawLine(start, end, color, duration, depthTest);
        }

        [Conditional(DebugDefine)]
        public static void DrawRay(Vector3 origin, Vector3 direction)
        {
            UnityEngine.Debug.DrawRay(origin, direction, Color.white, 0, true);
        }

        [Conditional(DebugDefine)]
        public static void DrawRay(Vector3 origin, Vector3 direction, Color color)
        {
            UnityEngine.Debug.DrawLine(origin, origin + direction, color, 0, true);
        }

        [Conditional(DebugDefine)]
        public static void DrawRay(Vector3 origin, Vector3 direction, Color color, float duration)
        {
            UnityEngine.Debug.DrawRay(origin, direction, color, duration, true);
        }

        [Conditional(DebugDefine)]
        public static void DrawRay(Vector3 origin, Vector3 direction, Color color, float duration, bool depthTest)
        {
            UnityEngine.Debug.DrawRay(origin, direction, color, duration, depthTest);
        }

        [Conditional(DebugDefine)]
        public static void DrawBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, Color color, float duration = 0f)
        {
            Vector3[] corners = new Vector3[8];
            GetBoxCorners(center, halfExtents, rotation, ref corners);

            // 아래 면
            UnityEngine.Debug.DrawLine(corners[0], corners[1], color, duration);
            UnityEngine.Debug.DrawLine(corners[1], corners[2], color, duration);
            UnityEngine.Debug.DrawLine(corners[2], corners[3], color, duration);
            UnityEngine.Debug.DrawLine(corners[3], corners[0], color, duration);

            // 위 면
            UnityEngine.Debug.DrawLine(corners[4], corners[5], color, duration);
            UnityEngine.Debug.DrawLine(corners[5], corners[6], color, duration);
            UnityEngine.Debug.DrawLine(corners[6], corners[7], color, duration);
            UnityEngine.Debug.DrawLine(corners[7], corners[4], color, duration);

            // 기둥
            for (int i = 0; i < 4; i++)
            {
                UnityEngine.Debug.DrawLine(corners[i], corners[i + 4], color, duration);
            }
        }

        [Conditional(DebugDefine)]
        public static void DrawBoxCast(Vector3 origin, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, Color color, float duration = 0f)
        {
            Vector3 centerStart = origin;
            Vector3 centerEnd = origin + direction.normalized * maxDistance;

            Vector3[] startPoints = new Vector3[8];
            Vector3[] endPoints = new Vector3[8];

            GetBoxCorners(centerStart, halfExtents, orientation, ref startPoints);
            GetBoxCorners(centerEnd, halfExtents, orientation, ref endPoints);

            // 한 박스의 연결 순서 정의 (윗면, 아랫면, 기둥 연결)
            DrawBoxEdges(startPoints, color, duration);
            DrawBoxEdges(endPoints, color, duration);

            // 앞뒤 연결선 (기둥)
            for (int i = 0; i < 8; i++)
            {
                UnityEngine.Debug.DrawLine(startPoints[i], endPoints[i], color, duration);
            }
        }

        [Conditional(DebugDefine)]
        private static void GetBoxCorners(Vector3 center, Vector3 halfExtents, Quaternion rotation, ref Vector3[] corners)
        {
            Vector3 right = rotation * Vector3.right * halfExtents.x;
            Vector3 up = rotation * Vector3.up * halfExtents.y;
            Vector3 forward = rotation * Vector3.forward * halfExtents.z;

            // 0~3: 아래면 (시계 방향)
            corners[0] = center + (-right - up - forward);
            corners[1] = center + (right - up - forward);
            corners[2] = center + (right - up + forward);
            corners[3] = center + (-right - up + forward);

            // 4~7: 윗면 (시계 방향)
            corners[4] = center + (-right + up - forward);
            corners[5] = center + (right + up - forward);
            corners[6] = center + (right + up + forward);
            corners[7] = center + (-right + up + forward);
        }

        [Conditional(DebugDefine)]
        private static void DrawBoxEdges(Vector3[] points, Color color, float duration)
        {
            // 아래면
            UnityEngine.Debug.DrawLine(points[0], points[1], color, duration);
            UnityEngine.Debug.DrawLine(points[1], points[2], color, duration);
            UnityEngine.Debug.DrawLine(points[2], points[3], color, duration);
            UnityEngine.Debug.DrawLine(points[3], points[0], color, duration);

            // 윗면
            UnityEngine.Debug.DrawLine(points[4], points[5], color, duration);
            UnityEngine.Debug.DrawLine(points[5], points[6], color, duration);
            UnityEngine.Debug.DrawLine(points[6], points[7], color, duration);
            UnityEngine.Debug.DrawLine(points[7], points[4], color, duration);

            // 기둥 연결
            for (int i = 0; i < 4; i++)
            {
                UnityEngine.Debug.DrawLine(points[i], points[i + 4], color, duration);
            }
        }

        [Conditional(DebugDefine)]
        public static void DrawBoxCast(Vector2 center, Vector2 size, float angleDeg, Color color, float duration = 0.1f)
        {
            Quaternion rotation = Quaternion.Euler(0, 0, angleDeg);
            Vector2 half = size * 0.5f;

            // 꼭짓점 구하기 (회전 적용)
            Vector2 topLeft = (Vector3)center + rotation * new Vector2(-half.x, half.y);
            Vector2 topRight = (Vector3)center + rotation * new Vector2(half.x, half.y);
            Vector2 bottomRight = (Vector3)center + rotation * new Vector2(half.x, -half.y);
            Vector2 bottomLeft = (Vector3)center + rotation * new Vector2(-half.x, -half.y);

            // 선 그리기
            UnityEngine.Debug.DrawLine(topLeft, topRight, color, duration);
            UnityEngine.Debug.DrawLine(topRight, bottomRight, color, duration);
            UnityEngine.Debug.DrawLine(bottomRight, bottomLeft, color, duration);
            UnityEngine.Debug.DrawLine(bottomLeft, topLeft, color, duration);
        }

        [Conditional(DebugDefine)]
        public static void DrawSector(Vector3 center, Vector3 forward, float radius, float angleDeg, Color? color = null, float duration = 0f, int segmentCount = 16)
        {
            if (color == null)
                color = Color.red;

            // Vector3 upOffset = Vector3.up * height;
            // center += upOffset;

            float halfAngle = angleDeg * 0.5f;
            float deltaAngle = angleDeg / segmentCount;

            // 시작 방향
            Quaternion startRotation = Quaternion.AngleAxis(-halfAngle, Vector3.up);
            Vector3 startDirection = startRotation * forward.normalized;

            Vector3 previousPoint = center + startDirection * radius;

            // 부채꼴 외곽 그리기
            for (int i = 1; i <= segmentCount; i++)
            {
                float currentAngle = -halfAngle + deltaAngle * i;
                Vector3 currentDir = Quaternion.Euler(0, currentAngle, 0) * forward.normalized;
                Vector3 currentPoint = center + currentDir * radius;

                UnityEngine.Debug.DrawLine(previousPoint, currentPoint, color.Value, duration);
                previousPoint = currentPoint;
            }

            // 부채꼴 중심선들
            Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * forward.normalized;
            Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * forward.normalized;
            UnityEngine.Debug.DrawLine(center, center + leftDir * radius, color.Value, duration);
            UnityEngine.Debug.DrawLine(center, center + rightDir * radius, color.Value, duration);
        }

        [Conditional(DebugDefine)]
        public static void DrawBox2D(Vector2 center, Vector2 size, float angleDeg, Color color = default, float duration = 0)
        {
            if (color == default)
                color = Color.red;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);

            // 반 크기
            Vector2 halfSize = size * 0.5f;

            // 박스의 4개 꼭짓점
            Vector2[] localCorners = new Vector2[4]
            {
                new Vector2(-halfSize.x, -halfSize.y),
                new Vector2(-halfSize.x, +halfSize.y),
                new Vector2(+halfSize.x, +halfSize.y),
                new Vector2(+halfSize.x, -halfSize.y)
            };

            // 회전 및 위치 적용
            Vector2[] worldCorners = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                var local = localCorners[i];
                float rotatedX = local.x * cos - local.y * sin;
                float rotatedY = local.x * sin + local.y * cos;
                worldCorners[i] = center + new Vector2(rotatedX, rotatedY);
            }

            // 네 개 변 그리기
            UnityEngine.Debug.DrawLine(worldCorners[0], worldCorners[1], color, duration);
            UnityEngine.Debug.DrawLine(worldCorners[1], worldCorners[2], color, duration);
            UnityEngine.Debug.DrawLine(worldCorners[2], worldCorners[3], color, duration);
            UnityEngine.Debug.DrawLine(worldCorners[3], worldCorners[0], color, duration);
        }

        [Conditional(DebugDefine)]
        public static void DrawCircle(Vector3 center, float radius, int segments = 32, Color color = default, float duration = 0f)
        {
            if (color == default)
            {
                color = Color.red;
            }

            float angleStep = 360f / segments;

            Vector3 prevPoint = center + new Vector3(Mathf.Cos(0), Mathf.Sin(0)) * radius;

            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                UnityEngine.Debug.DrawLine(prevPoint, nextPoint, color, duration);
                prevPoint = nextPoint;
            }
        }
    }
}