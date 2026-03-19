using System;
using UnityEngine;

namespace DragonGate
{
    public sealed class PoolHandle<T> where T : Component
    {
        private readonly IComponentPoolCore<T> _pool;
        private bool _isDestroyed = false;

        internal PoolHandle(IComponentPoolCore<T> pool)
        {
            _pool = pool;
        }

        public bool IsDestroyed => _isDestroyed;

        public T Get()
        {
            ThrowIfDestroyed();
            T component = _pool.Get();
            if (component == null) return null;

            Action returnAction = BuildReturnAction(component);
            IPoolable poolable = ResolvePoolable(component);

            if (poolable is PoolScopeAutoReturn autoReturn)
                autoReturn.SetReturnAction(returnAction);

            poolable?.OnGet();
            return component;
        }

        public T Get(float lifetime)
        {
            ThrowIfDestroyed();
            if (lifetime < 0f) lifetime = 0f;

            T component = _pool.Get();
            if (component == null) return null;

            Action returnAction = BuildReturnAction(component);

            PoolScopeAutoReturn autoReturn = component.gameObject.GetComponent<PoolScopeAutoReturn>();
            if (autoReturn == null)
                autoReturn = component.gameObject.AddComponent<PoolScopeAutoReturn>();

            autoReturn.SetReturnAction(returnAction);

            IPoolable poolable = ResolvePoolable(component);
            poolable?.OnGet();

            autoReturn.Activate(lifetime, returnAction);
            return component;
        }

        public void Return(T component)
        {
            ThrowIfDestroyed();
            _pool.Return(component);
        }

        public void Clear()
        {
            ThrowIfDestroyed();
            _pool.ClearObjects();
        }

        public void Destroy()
        {
            ThrowIfDestroyed();
            _pool.DestroyPool();
            _isDestroyed = true;
        }

        private IPoolable ResolvePoolable(T component)
        {
            if (component is IPoolable poolable)
                return poolable;
            return component.gameObject.GetComponent<IPoolable>();
        }

        private Action BuildReturnAction(T component)
        {
            return () =>
            {
                if (component != null && _isDestroyed == false)
                    _pool.Return(component);
            };
        }

        private void ThrowIfDestroyed()
        {
            if (_isDestroyed)
                throw new InvalidOperationException($"[PoolScope] PoolHandle<{typeof(T).Name}> has already been destroyed.");
        }
    }

    internal interface IComponentPoolCore<T> where T : Component
    {
        T Get();
        void Return(T component);
        void ClearObjects();
        void DestroyPool();
    }

    internal interface IComponentPoolAutoReturn
    {
        void TryReturn(Component component);
    }
}
