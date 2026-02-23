using UnityEngine;

namespace DragonGate
{
    public interface IColliderEventReceiver
    {
        public virtual void OnTriggerEnterEvent(IHittable hittable, Collider collider)
        {

        }

        public virtual void OnHit(IHittable hittable, Collider2D collider)
        {

        }

        public virtual void OnTriggerExitEvent(IHittable hittable, Collider collider)
        {

        }

        public virtual void OnTriggerExitEvent2D(IHittable hittable, Collider2D collider)
        {

        }

        public virtual void OnTriggerStayEvent(IHittable hittable, Collider collider)
        {

        }

        public virtual void OnTriggerStayEvent2D(IHittable hittable, Collider2D collider)
        {

        }

        public virtual void OnCollisionEnterEvent(IHittable hittable, Collision collision)
        {

        }

        public virtual void OnCollisionEnterEvent2D(IHittable hittable, Collision2D collision)
        {

        }

        public virtual void OnCollisionExitEvent(IHittable hittable, Collision collision)
        {

        }

        public virtual void OnCollisionExitEvent2D(IHittable hittable, Collision2D collision)
        {

        }

        public virtual void OnCollisionStayEvent(IHittable hittable, Collision collision)
        {

        }

        public virtual void OnCollisionStayEvent2D(IHittable hittable, Collision2D collision)
        {

        }
    }
}