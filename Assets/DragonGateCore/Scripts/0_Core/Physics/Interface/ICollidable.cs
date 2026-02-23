using UnityEngine;

namespace DragonGate
{
    public interface ICollidable
    {
        public Vector3 GetPosition();
        public Vector3 GetForward();
        public void OnBeEnter();
        public virtual void OnBeStay() { }
        public virtual void OnBeExit() { }
        public void OnEnter();
        public virtual void OnStay() { }
        public virtual void OnExit() { }
    }
}