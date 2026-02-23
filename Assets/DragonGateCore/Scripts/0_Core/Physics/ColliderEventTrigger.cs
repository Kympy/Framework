using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    /// <summary>
    ///  기본적으로 hittable 을 가져올 때는 collider 와 같은 오브젝트에서 가져온다. 
    /// </summary>
    /// <exception cref="NullReferenceException"></exception>
    /// <exception cref="InvalidCastException"></exception>
    public abstract class ColliderEventTrigger : MonoBehaviour
    {
        [Header("Event Target Parent")]
        [Tooltip("Parent object that implements IColliderEventReceiver to handle events.")]
        [SerializeField] protected MonoBehaviour _parent;

        private IColliderEventReceiver _receiver;
        private Dictionary<int, IHittable> _hitObjectCache = new Dictionary<int, IHittable>();

        protected virtual void Awake()
        {
            if (_parent == null)
            {
                throw new NullReferenceException($"ColliderEventTrigger {gameObject.name} requires a receiver to be set.");
            }

            if (_parent is IColliderEventReceiver receiverInterface)
            {
                _receiver = receiverInterface;
            }
            else
            {
                throw new InvalidCastException($"Receiver {_parent.GetType().Name} does not implement IColliderEventReceiver.");
            }
        }

        public virtual void OnTriggerEnter(Collider collider)
        {
            var hittable = GetHittable(collider);
            if (hittable == null) return;
            hittable.OnEnter();
            _receiver.OnTriggerEnterEvent(hittable, collider);
        }

        public virtual void OnTriggerEnter2D(Collider2D collider)
        {
            UnityEngine.Debug.Log("Enter");
            var hittable = GetHittable(collider);
            if (hittable == null) return;
            hittable.OnEnter();
            _receiver.OnHit(hittable, collider);
        }

        public virtual void OnTriggerExit(Collider collider)
        {
            var hittable = GetHittable(collider);
            if (hittable == null) return;
            hittable.OnExit();
            _receiver.OnTriggerExitEvent(hittable, collider);
        }

        public virtual void OnTriggerExit2D(Collider2D collider)
        {
            var hittable = GetHittable(collider);
            if (hittable == null) return;
            hittable.OnExit();
            _receiver.OnTriggerExitEvent2D(hittable, collider);
        }

        public virtual void OnTriggerStay(Collider collider)
        {
            var hittable = GetHittable(collider);
            if (hittable == null) return;
            hittable.OnStay();
            _receiver.OnTriggerStayEvent(hittable, collider);
        }

        public virtual void OnTriggerStay2D(Collider2D collider)
        {
            var hittable = GetHittable(collider);
            if (hittable == null) return;
            hittable.OnStay();
            _receiver.OnTriggerStayEvent2D(hittable, collider);
        }

        public virtual void OnCollisionEnter(Collision collision)
        {
            var hittable = GetHittable(collision);
            if (hittable == null) return;
            hittable.OnEnter();
            _receiver.OnCollisionEnterEvent(hittable, collision);
        }

        public virtual void OnCollisionEnter2D(Collision2D collision)
        {
            var hittable = GetHittable(collision);
            if (hittable == null) return;
            hittable.OnEnter();
            _receiver.OnCollisionEnterEvent2D(hittable, collision);
        }

        public virtual void OnCollisionExit(Collision collision)
        {
            var hittable = GetHittable(collision);
            if (hittable == null) return;
            hittable.OnExit();
            _receiver.OnCollisionExitEvent(hittable, collision);
        }

        public virtual void OnCollisionExit2D(Collision2D collision)
        {
            var hittable = GetHittable(collision);
            if (hittable == null) return;
            hittable.OnExit();
            _receiver.OnCollisionExitEvent2D(hittable, collision);
        }

        public virtual void OnCollisionStay(Collision collision)
        {
            var hittable = GetHittable(collision);
            if (hittable == null) return;
            hittable.OnStay();
            _receiver.OnCollisionStayEvent(hittable, collision);
        }

        public virtual void OnCollisionStay2D(Collision2D collision)
        {
            var hittable = GetHittable(collision);
            if (hittable == null) return;
            hittable.OnStay();
            _receiver.OnCollisionStayEvent2D(hittable, collision);
        }

        private IHittable GetHittable(Collider collider)
        {
            if (collider == null) return null;
            int instanceId = collider.GetInstanceID();
            if (_hitObjectCache.TryGetValue(instanceId, out IHittable hittable))
            {
                return hittable;
            }

            if (collider.TryGetComponent(out hittable) == false)
            {
                hittable = collider.GetComponentInChildren<IHittable>();
                if (hittable == null)
                {
                    throw new System.Exception($"Collider {collider.gameObject.name} does not have a hittable.");
                }
            }

            _hitObjectCache[instanceId] = hittable;
            return hittable;
        }

        private IHittable GetHittable(Collider2D collider)
        {
            if (collider == null) return null;
            int instanceId = collider.GetInstanceID();
            if (_hitObjectCache.TryGetValue(instanceId, out IHittable hittable))
            {
                return hittable;
            }

            if (collider.TryGetComponent(out hittable) == false)
            {
                hittable = collider.GetComponentInChildren<IHittable>();
                if (hittable == null)
                {
                    throw new System.Exception($"Collider {collider.gameObject.name} does not have a hittable.");
                }
            }

            _hitObjectCache[instanceId] = hittable;
            return hittable;
        }

        private IHittable GetHittable(Collision collision)
        {
            if (collision == null) return null;

            int instanceId = collision.collider.GetInstanceID();
            if (_hitObjectCache.TryGetValue(instanceId, out IHittable hittable))
            {
                return hittable;
            }

            if (collision.collider.TryGetComponent(out hittable) == false)
            {
                hittable = collision.collider.GetComponentInChildren<IHittable>();
                if (hittable == null)
                {
                    throw new System.Exception($"Collider {collision.gameObject.name} does not have a hittable.");
                }
            }

            _hitObjectCache[instanceId] = hittable;
            return hittable;
        }

        private IHittable GetHittable(Collision2D collision)
        {
            if (collision == null) return null;

            int instanceId = collision.collider.GetInstanceID();
            if (_hitObjectCache.TryGetValue(instanceId, out IHittable hittable))
            {
                return hittable;
            }

            if (collision.collider.TryGetComponent(out hittable) == false)
            {
                hittable = collision.collider.GetComponentInChildren<IHittable>();
                if (hittable == null)
                {
                    throw new System.Exception($"Collider {collision.gameObject.name} does not have a hittable.");
                }
            }

            _hitObjectCache[instanceId] = hittable;
            return hittable;
        }
    }
}
