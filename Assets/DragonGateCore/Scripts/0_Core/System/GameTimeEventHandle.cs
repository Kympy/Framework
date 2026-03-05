using System;

namespace DragonGate
{
    public class GameTimeEventHandle
    {
        public long TargetTick { get; internal set; }
        internal Action<long> Callback { get; }

        internal GameTimeEventHandle(long targetTick, Action<long> callback)
        {
            TargetTick = targetTick;
            Callback = callback;
        }
    }

    public class GameTimeRepeatingEventHandle : GameTimeEventHandle
    {
        internal long IntervalTick { get; }

        internal GameTimeRepeatingEventHandle(long firstFireTick, long intervalTick, Action<long> callback)
            : base(firstFireTick, callback)
        {
            IntervalTick = intervalTick;
        }
    }
}