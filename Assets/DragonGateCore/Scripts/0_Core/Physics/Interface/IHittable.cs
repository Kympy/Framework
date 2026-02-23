namespace DragonGate
{
    public interface IHittable : ICollidable
    {
        public void Unregister();
    }
}