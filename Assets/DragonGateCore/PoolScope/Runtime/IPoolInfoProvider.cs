using UnityEngine;

namespace DragonGate
{
    public interface IPoolInfoProvider
    {
        string PoolName { get; }
        int TotalCount { get; }
        int LeftInPool { get; }
        int CurrentInUse { get; }
        int PeakUsage { get; }
        Color BarColor { get; }
    }
}
