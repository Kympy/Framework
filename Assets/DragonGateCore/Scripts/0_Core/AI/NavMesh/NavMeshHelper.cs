using UnityEngine;
using UnityEngine.AI;

namespace DragonGate
{
    public class NavMeshHelper
    {
        private static NavMeshPath _path = new();
        
        public static bool HasArrived(NavMeshAgent agent)
        {
            if (agent.pathPending)
                return false;

            if (agent.remainingDistance <= agent.stoppingDistance)
                return true;

            return false;
        }
        
        public static Vector3? FindClosestAvailablePosition(NavMeshAgent agent, NavMeshAgent targetNpc, float tolerance = 0.2f)
        {
            float baseDistance = targetNpc.radius + agent.radius + tolerance;
            int searchCount = 12;

            for (int i = 0; i < searchCount; i++)
            {
                float angle = (360f / searchCount) * i;

                Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;

                Vector3 candidate = targetNpc.transform.position + direction * baseDistance;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }
            DGDebug.Log("Unable to find closest target position.", Color.antiqueWhite);
            return null;
        }
        
        public static bool TryGetReachablePosition(Vector3 desiredWorldPosition, out Vector3 correctedPosition, float searchRadius = 1f, int areaMask = NavMesh.AllAreas)
        {
            bool found = NavMesh.SamplePosition(
                desiredWorldPosition,
                out var navMeshHit,
                searchRadius,
                areaMask);

            if (found)
            {
                correctedPosition = navMeshHit.position;
                return true;
            }

            correctedPosition = desiredWorldPosition;
            return false;
        }
        
        public static bool IsReachable(Vector3 startPosition, Vector3 targetPosition)
        {
            ThreadHelper.EnsureMainThread();
            bool pathExists = NavMesh.CalculatePath(startPosition, targetPosition, NavMesh.AllAreas, _path);
            return pathExists && _path.status == NavMeshPathStatus.PathComplete;
        }
        
        public static bool TryGetRandomPosition(
            Vector3 center,
            float searchRadius,
            out Vector3 resultPosition,
            int maxAttemptCount = 30)
        {
            for (int attemptIndex = 0; attemptIndex < maxAttemptCount; attemptIndex++)
            {
                Vector3 randomPoint =
                    center + Random.insideUnitSphere * searchRadius;

                if (NavMesh.SamplePosition(
                        randomPoint,
                        out NavMeshHit navMeshHit,
                        searchRadius,
                        NavMesh.AllAreas))
                {
                    resultPosition = navMeshHit.position;
                    return true;
                }
            }

            resultPosition = Vector3.zero;
            return false;
        }
    }
}
