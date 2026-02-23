using System.Buffers;
using UnityEngine;

namespace DragonGate
{
    public static class CollisionChecker
    {
        public const float CircularSectorCheckTolerance = 0.01f;
        public static int CheckCircularSector(Transform transform, float radius, float angle, float height, Collider[] colliders, int layerMask)
        {
            Vector3 center = transform.position;
            Vector3 forward = transform.forward;
            return CheckCircularSectorInternal(center, forward, radius, angle, colliders, height, layerMask);
        }
        
        public static int CheckCircularSector(Transform transform, float radius, float angle, Collider[] results)
        {
            Vector3 center = transform.position;
            Vector3 forward = transform.forward;
            return CheckCircularSectorInternal(center, forward, radius, angle, results);
        }

        public static int CheckCircularSector(Transform transform, float radius, float angle, Collider[] results, int layerMask)
        {
            Vector3 center = transform.position;
            Vector3 forward = transform.forward;
            return CheckCircularSectorInternal(center, forward, radius, angle, results, 0, layerMask);
        }

        public static int CheckCircularSector(Vector3 center, Vector3 forward, float radius, float angle, Collider[] colliders, int layerMask)
        {
            return CheckCircularSectorInternal(center, forward, radius, angle, colliders, 0, layerMask);
        }

        private static int CheckCircularSectorInternal(Vector3 center, Vector3 forward, float radius, float angle, Collider[] results, float height = 0, int layerMask = ~0)
        {
            // 1. 먼저 반지름 내의 모든 콜라이더 검색
            var colliders = ArrayPool<Collider>.Shared.Rent(results.Length);
            center += Vector3.up * height;
            int hitCount = Physics.OverlapSphereNonAlloc(center, radius, colliders, layerMask);
            if (hitCount == 0)
            {
                ArrayPool<Collider>.Shared.Return(colliders);
                return 0;
            }
            
            float halfAngle = angle * 0.5f;
            float halfAngleRad = halfAngle * Mathf.Deg2Rad;

            int resultIndex = 0;
            for (int i = 0; i < hitCount; i++)
            {
                var collider = colliders[i];
                Vector3 direction = collider.transform.position - center;
                direction.y = 0f;
                // zero zone
                if (direction.sqrMagnitude < CircularSectorCheckTolerance)
                    continue;
                
                float dot = Vector3.Dot(forward.normalized, direction.normalized);
                if (dot >= Mathf.Cos(halfAngleRad))
                {
                    // 각도 안에 있음
                    results[resultIndex] = collider;
                    resultIndex++;
                }
            }
            ArrayPool<Collider>.Shared.Return(colliders, true);
            return resultIndex;
        }
        
        public static bool IsPointInsideCircle(Vector3 point, Vector3 center, float radius)
        {
            float sqrDistance = (point - center).sqrMagnitude;
            return sqrDistance <= radius * radius;
        }
    }
}