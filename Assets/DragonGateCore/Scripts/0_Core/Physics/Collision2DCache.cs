using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    public class Collision2DCache
    {
        private static Lazy<Dictionary<int, ICollidable>> _colliderCache = new Lazy<Dictionary<int, ICollidable>>(() => new Dictionary<int, ICollidable>());

        public static T GetOrAdd<T>(Collider2D collider) where T : class, ICollidable
        {
            int instanceId = collider.GetInstanceID();
            if (_colliderCache.Value.TryGetValue(instanceId, out ICollidable hittable))
            {
                return hittable as T;
            }
            if (collider.TryGetComponent(out hittable) == false)
            {
                throw new System.Exception("No hittable found");
            }
            _colliderCache.Value.Add(instanceId, hittable);
            return hittable as T;
        }

        public static void UnregisterCollider(Collider2D collider)
        {
            if (_colliderCache.IsValueCreated)
                _colliderCache.Value.Remove(collider.GetInstanceID());
        }

        public static void ClearCache()
        {
            if (_colliderCache.IsValueCreated)
                _colliderCache.Value.Clear();
        }
    }
}