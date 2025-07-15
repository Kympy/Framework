namespace Framework
{
    public interface ITickable
    {
        public void Register();
        public void UnRegister();
    }
    
    public interface ITick : ITickable
    {
        public void Tick(float deltaTime);
        void ITickable.Register()
        {
            TickManager.Instance.Register(this);
        }
        void ITickable.UnRegister()
        {
            TickManager.Instance.Unregister(this);
        }
    }

    public interface IFixedTick : ITickable
    {
        public void FixedTick(float fixedDeltaTime);
        void ITickable.Register()
        {
            TickManager.Instance.Register(this);
        }
        void ITickable.UnRegister()
        {
            TickManager.Instance.Unregister(this);
        }
    }
    
    public interface ILateTick : ITickable
    {
        public void LateTick(float deltaTime);
        void ITickable.Register()
        {
            TickManager.Instance.Register(this);
        }
        void ITickable.UnRegister()
        {
            TickManager.Instance.Unregister(this);
        }
    }
}
