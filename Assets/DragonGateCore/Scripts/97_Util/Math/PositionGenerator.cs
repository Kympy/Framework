using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    public static class PositionGenerator
    {
        // 엣지 순서 (시계방향 기준)
        private static Vector2[] _edgeCenters = new[]
        {
            new Vector2(0.5f, 1f), // top
            new Vector2(1f, 0.5f), // right
            new Vector2(0.5f, 0f), // bottom
            new Vector2(0f, 0.5f)  // left
        };
        
        // 엣지 순서대로 Viewport 좌표 구간 (시계방향 기준)
        private static Vector2[] _corners = new[]
        {
            new Vector2(1, 1), // top-right
            new Vector2(1, 0), // bottom-right
            new Vector2(0, 0), // bottom-left
            new Vector2(0, 1)  // top-left
        };

        public static void GetCirclePositionsXZ(Vector3 center, float radius, ref Vector3[] positions, int count = 0)
        {
            int pointCount = count <= 0 ? positions.Length : count;
            float step = Mathf.PI * 2f / pointCount;
            for (int i = 0 ; i < pointCount ; i++)
            {
                float theta = i * step;
                var x = radius * Mathf.Cos(theta);
                var z = radius * Mathf.Sin(theta);
                positions[i] = center + new Vector3(x, 0, z);
            }
        }
        
        public static void GetCirclePositionsXY(Vector3 center, float radius, ref Vector3[] positions, int count = 0)
        {
            int pointCount = count <= 0 ? positions.Length : count;
            float step = Mathf.PI * 2f / pointCount;
            for (int i = 0 ; i < pointCount ; i++)
            {
                float theta = i * step;
                var x = radius * Mathf.Cos(theta);
                var y = radius * Mathf.Sin(theta);
                positions[i] = center + new Vector3(x, y, 0);
            }
        }

        public static Vector3 GetRandomScreenPosition2D(Camera camera, float threshold = 0.95f)
        {
            // 2. Viewport 좌표(0~1 사이) 중에서, 안전한 범위 내 랜덤 포인트
            Vector2 randomViewportPos = new Vector2(
                Random.Range(1 - threshold, threshold), // x: 좌우 여백 10%
                Random.Range(1 - threshold, threshold)  // y: 상하 여백 10%
            );

            // 3. Viewport → World 변환
            var targetPosition = camera.ViewportToWorldPoint(new Vector3(randomViewportPos.x, randomViewportPos.y, 0));
            targetPosition.z = 0f;
            return targetPosition;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="threshold"> 1 : 1까지 전부 사용, 0.9 : 0.1 ~ 0.9 범위만 사용 </param>
        /// <returns></returns>
        public static Vector3 GetRandomScreenEdgePosition2D(Camera camera, float threshold = 0.95f)
        {
            // 1. 랜덤하게 화면의 어떤 엣지인지 고름 (0: top, 1: bottom, 2: left, 3: right)
            int edge = Random.Range(0, 4);
    
            Vector2 viewportPos;

            float min = 1f - threshold;
            float max = threshold;

            switch (edge)
            {
                case 0: // top
                    viewportPos = new Vector2(Random.Range(min, max), max);
                    break;
                case 1: // bottom
                    viewportPos = new Vector2(Random.Range(min, max), min);
                    break;
                case 2: // left
                    viewportPos = new Vector2(min, Random.Range(min, max));
                    break;
                case 3: // right
                    viewportPos = new Vector2(max, Random.Range(min, max));
                    break;
                default:
                    viewportPos = new Vector2(0.5f, 0.5f);
                    break;
            }

            // Viewport → World 변환
            var worldPos = camera.ViewportToWorldPoint(new Vector3(viewportPos.x, viewportPos.y, camera.nearClipPlane));
            worldPos.z = 0f;
            return worldPos;
        }
        
        public static void GetEdgePath2D(Camera camera, List<Vector3> result, float threshold = 0.95f, bool clockwise = true)
        {
            result.Clear();

            // threshold 보정
            for (int i = 0; i < _corners.Length; i++)
            {
                _corners[i].x = Mathf.Lerp(1f - threshold, threshold, _corners[i].x);
                _corners[i].y = Mathf.Lerp(1f - threshold, threshold, _corners[i].y);
            }

            // 현재 시작점
            Vector3 startWorld = GetRandomScreenEdgePosition2D(camera, threshold);

            // Viewport 상 좌표로 변환
            Vector3 startViewport = camera.WorldToViewportPoint(startWorld);

            // 어느 엣지인지 찾기
            int startEdge = FindClosestEdge(startViewport);

            // 이동 경로 구성
            int dir = clockwise ? 1 : -1;

            for (int i = 0; i < 4; i++)
            {
                int index = (startEdge + i * dir + 4) % 4;
                Vector2 vp = _corners[index];
                Vector3 world = camera.ViewportToWorldPoint(new Vector3(vp.x, vp.y, camera.nearClipPlane));
                world.z = 0;
                result.Add(world);
            }
        }

        private static int FindClosestEdge(Vector2 viewport)
        {
            int closest = 0;
            float minSqrDist = float.MaxValue;

            for (int i = 0; i < _edgeCenters.Length; i++)
            {
                float sqrDist = (viewport - _edgeCenters[i]).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    closest = i;
                }
            }
            return closest;
        }
        
        public static Vector3 GetNextEdgePoint2D(Camera camera, Vector3 currentPositionWorld, float threshold = 0.95f, bool clockwise = true)
        {
            Vector3 viewportPoint = camera.WorldToViewportPoint(currentPositionWorld);

            // 현재 위치 기준 가장 가까운 엣지 판단
            int currentEdge = FindClosestEdge(viewportPoint);

            // 다음 엣지
            int dir = clockwise ? 1 : -1;
            int nextEdge = (currentEdge + dir + 4) % 4;

            float min = 1f - threshold;
            float max = threshold;

            Vector2 vpPos;
            switch (nextEdge)
            {
                case 0: // top
                    vpPos = new Vector2(Random.Range(min, max), max);
                    break;
                case 1: // right
                    vpPos = new Vector2(max, Random.Range(min, max));
                    break;
                case 2: // bottom
                    vpPos = new Vector2(Random.Range(min, max), min);
                    break;
                case 3: // left
                    vpPos = new Vector2(min, Random.Range(min, max));
                    break;
                default:
                    vpPos = new Vector2(0.5f, 0.5f); // fallback
                    break;
            }

            Vector3 world = camera.ViewportToWorldPoint(new Vector3(vpPos.x, vpPos.y, camera.nearClipPlane));
            world.z = 0f;
            return world;
        }
        
        public static Vector3 GetNearestEdgeKeepingPosition(Camera cam, Vector3 worldPos, float threshold = 0.95f)
        {
            Vector3 vp = cam.WorldToViewportPoint(worldPos);

            float min = 1f - threshold;
            float max = threshold;
            
            // Clamp the coordinate we want to preserve into the threshold window as well
            float clampedX = Mathf.Clamp(vp.x, min, max);
            float clampedY = Mathf.Clamp(vp.y, min, max);
            
            int closestEdge = FindClosestEdge(vp);

            Vector2 resultVp;

            switch (closestEdge)
            {
                case 0: // top
                    resultVp = new Vector2(clampedX, max);
                    break;
                case 1: // right
                    resultVp = new Vector2(max, clampedY);
                    break;
                case 2: // bottom
                    resultVp = new Vector2(clampedX, min);
                    break;
                case 3: // left
                    resultVp = new Vector2(min, clampedY);
                    break;
                default:
                    resultVp = vp;
                    break;
            }

            Vector3 resultWorld = cam.ViewportToWorldPoint(new Vector3(resultVp.x, resultVp.y, vp.z));
            resultWorld.z = worldPos.z;
            return resultWorld;
        }
    }
}