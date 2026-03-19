using System;

namespace DragonGate
{
    /// <summary>
    /// A handle to a class object pool created via PoolScope.CreateClassPool&lt;T&gt;.
    /// </summary>
    public sealed class ClassPoolHandle<T> where T : class, new()
    {
        private readonly IClassPoolCore<T> _pool;
        private bool _isDestroyed = false;

        internal ClassPoolHandle(IClassPoolCore<T> pool)
        {
            _pool = pool;
        }

        public bool IsDestroyed => _isDestroyed;

        /// <summary>
        /// Gets an instance from the pool. Creates a new one if the pool is empty.
        /// </summary>
        public T Get()
        {
            ThrowIfDestroyed();
            return _pool.Get();
        }

        /// <summary>
        /// Returns an instance to the pool.
        /// </summary>
        public void Return(T instance)
        {
            ThrowIfDestroyed();
            _pool.Return(instance);
        }

        /// <summary>
        /// Clears all pooled instances. The handle remains valid.
        /// </summary>
        public void Clear()
        {
            ThrowIfDestroyed();
            _pool.ClearObjects();
        }

        /// <summary>
        /// Clears the pool, unregisters from the monitor, and invalidates this handle.
        /// </summary>
        public void Destroy()
        {
            ThrowIfDestroyed();
            _pool.DestroyPool();
            _isDestroyed = true;
        }

        private void ThrowIfDestroyed()
        {
            if (_isDestroyed)
                throw new InvalidOperationException($"[PoolScope] ClassPoolHandle<{typeof(T).Name}> has already been destroyed.");
        }
    }

    internal interface IClassPoolCore<T> where T : class, new()
    {
        T Get();
        void Return(T instance);
        void ClearObjects();
        void DestroyPool();
    }
}
