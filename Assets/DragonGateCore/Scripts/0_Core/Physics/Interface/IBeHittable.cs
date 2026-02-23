namespace DragonGate
{
    public interface IBeHittable : ICollidable
    {
        public void UnregisterCollider();
    }
}