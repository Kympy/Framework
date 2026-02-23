using UnityEngine;
using UnityEngine.AI;

namespace DragonGate
{
    public class NavMeshHelper
    {
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
    }
}
