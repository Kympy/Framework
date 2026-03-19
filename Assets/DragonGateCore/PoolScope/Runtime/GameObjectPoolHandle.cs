using UnityEngine;

namespace DragonGate
{
    /// <summary>
    /// A handle to a plain GameObject pool created via PoolScope.CreateObjectPool.
    /// Wraps PoolHandle&lt;Transform&gt; and exposes a GameObject-specific API.
    /// </summary>
    public sealed class GameObjectPoolHandle
    {
        private readonly PoolHandle<Transform> _innerHandle;

        internal GameObjectPoolHandle(PoolHandle<Transform> innerHandle)
        {
            _innerHandle = innerHandle;
        }

        public bool IsDestroyed => _innerHandle.IsDestroyed;

        /// <summary>
        /// Gets a GameObject from the pool.
        /// </summary>
        public GameObject GetObject()
        {
            Transform t = _innerHandle.Get();
            return t != null ? t.gameObject : null;
        }

        /// <summary>
        /// Gets a GameObject from the pool and auto-returns it after the given lifetime (seconds).
        /// </summary>
        public GameObject GetObject(float lifetime)
        {
            Transform t = _innerHandle.Get(lifetime);
            return t != null ? t.gameObject : null;
        }

        /// <summary>
        /// Returns a GameObject to the pool.
        /// </summary>
        public void ReturnObject(GameObject pooledObject)
        {
            if (pooledObject == null) return;
            _innerHandle.Return(pooledObject.transform);
        }

        /// <summary>
        /// Destroys all pooled objects not currently in use. Handle remains valid.
        /// </summary>
        public void Clear() => _innerHandle.Clear();

        /// <summary>
        /// Destroys all pooled objects and invalidates this handle.
        /// </summary>
        public void Destroy() => _innerHandle.Destroy();
    }
}
