using UnityEngine;

namespace DragonGate
{
    /// <summary>
    /// RaycastNonAlloc 결과에서 IRaycastPriority 기준으로 최적 후보를 선정한다.
    /// IRaycastPriority 구현체가 있으면 가장 높은 priority를 선택하고, 동점이면 거리 기준.
    /// 구현체가 없으면 가장 가까운 hit을 fallback으로 반환한다.
    /// </summary>
    public static class RaycastPriorityResolver
    {
        public static bool TryResolve(RaycastHit[] hits, int hitCount, out RaycastHit result)
        {
            result = default;
            if (hitCount <= 0) return false;

            bool hasPriorityCandidate = false;
            int bestPriority = int.MinValue;
            float bestDistance = float.MaxValue;
            int bestIndex = -1;

            for (int i = 0; i < hitCount; i++)
            {
                if (hits[i].transform.TryGetComponent(out IRaycastPriority priorityComp))
                {
                    int priority = priorityComp.RaycastPriority;
                    bool betterPriority = priority > bestPriority;
                    bool samePriorityCloser = priority == bestPriority && hits[i].distance < bestDistance;
                    if (betterPriority || samePriorityCloser)
                    {
                        bestPriority = priority;
                        bestDistance = hits[i].distance;
                        bestIndex = i;
                        hasPriorityCandidate = true;
                    }
                }
            }

            if (hasPriorityCandidate)
            {
                result = hits[bestIndex];
                return true;
            }

            // IRaycastPriority 구현체 없음 → 가장 가까운 hit으로 fallback
            bestDistance = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                if (hits[i].distance < bestDistance)
                {
                    bestDistance = hits[i].distance;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0)
            {
                result = hits[bestIndex];
                return true;
            }

            return false;
        }
    }
}