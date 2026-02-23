namespace DragonGate
{
    public struct TimerHandle
    {
        public uint Id;

        public void Clear()
        {
            TimerManager.Clear(this);
        }
    }
}