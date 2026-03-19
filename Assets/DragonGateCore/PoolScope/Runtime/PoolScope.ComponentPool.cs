using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DragonGate
{
    public partial class PoolScope
    {
        // instanceId → pool, for PoolScope.Return(component) convenience
        private readonly Dictionary<int, IComponentPoolAutoReturn> _instanceIdToPool = new Dictionary<int, IComponentPoolAutoReturn>();

        private int _componentPoolSequence = 0;

        internal PoolHandle<T> CreatePoolInternal<T>(PoolScopeLoader loader, int initialCount) where T : Component
        {
            _componentPoolSequence++;
            string monitorKey = $"{typeof(T).Name} #{_componentPoolSequence}";

            ComponentPool<T> pool = new ComponentPool<T>(monitorKey, loader, gameObject.transform, initialCount, _instanceIdToPool, this);
            RegisterToMonitor(monitorKey, pool);
            return new PoolHandle<T>(pool);
        }

        private void ReturnComponentInternal(Component component)
        {
            if (component == null) return;
            if (_isApplicationQuitting) return;

            int instanceId = component.gameObject.GetInstanceID();
            if (_instanceIdToPool.TryGetValue(instanceId, out IComponentPoolAutoReturn pool) == false)
            {
                Debug.LogError($"[PoolScope] '{component.name}' was not obtained from any pool, or was already returned.");
                Object.Destroy(component.gameObject);
                return;
            }

            pool.TryReturn(component);
        }

        private void ClearAllComponentPools()
        {
            // Individual pools are owned by handles; ClearAll just wipes tracking data.
            _instanceIdToPool.Clear();
        }

        // ----------------------------------------------------------------
        // ComponentPool inner class
        // ----------------------------------------------------------------

        private sealed class ComponentPool<T> : IComponentPoolCore<T>, IComponentPoolAutoReturn, IPoolInfoProvider where T : Component
        {
            private readonly Stack<T> _pool = new Stack<T>();
            private readonly string _monitorKey;
            private readonly PoolScopeLoader _loader;
            private readonly GameObject _rootObject;
            private readonly Dictionary<int, IComponentPoolAutoReturn> _instanceTracker;
            private readonly PoolScope _owner;

            private int _currentInUse = 0;
            private int _peakUsage = 0;
            private readonly Color _barColor = PoolScopeColor.GetNextColor();

            public string PoolName => _monitorKey;
            public int TotalCount => _pool.Count + _currentInUse;
            public int LeftInPool => _pool.Count;
            public int CurrentInUse => _currentInUse;
            public int PeakUsage => _peakUsage;
            public Color BarColor => _barColor;

            public ComponentPool(string monitorKey, PoolScopeLoader loader, Transform parent, int initialCount,
                Dictionary<int, IComponentPoolAutoReturn> instanceTracker, PoolScope owner)
            {
                _monitorKey = monitorKey;
                _loader = loader;
                _instanceTracker = instanceTracker;
                _owner = owner;

                _rootObject = new GameObject($"[Pool] {monitorKey}");
                _rootObject.transform.SetParent(parent);

                for (int i = 0; i < initialCount; i++)
                    CreateAndPush();
            }

            private void CreateAndPush()
            {
                GameObject newObject = _loader.Instantiate();
                if (newObject == null)
                {
                    Debug.LogError($"[PoolScope] Loader returned null for pool '{_monitorKey}'.");
                    return;
                }

                T component = newObject.GetComponent<T>();
                if (component == null)
                {
                    Debug.LogError($"[PoolScope] Component {typeof(T).Name} not found on instantiated object in pool '{_monitorKey}'.");
                    Object.Destroy(newObject);
                    return;
                }

                newObject.transform.SetParent(_rootObject.transform);
                newObject.SetActive(false);
                _pool.Push(component);
            }

            public T Get()
            {
                if (_pool.Count == 0)
                    CreateAndPush();

                if (_pool.Count == 0) return null;

                T component = _pool.Pop();
                component.transform.SetParent(null);
                component.gameObject.SetActive(true);

                // OnGet() is intentionally NOT called here.
                // PoolHandle.Get() calls it after wiring the return action,
                // so PoolScopeAutoReturn.OnGet() can safely access _returnAction.

                _currentInUse++;
                if (_currentInUse > _peakUsage)
                    _peakUsage = _currentInUse;

                _instanceTracker[component.gameObject.GetInstanceID()] = this;
                return component;
            }

            public void Return(T component)
            {
                if (component == null) return;

                IPoolable poolable = component is IPoolable p ? p : component.gameObject.GetComponent<IPoolable>();
                poolable?.OnReturn();

                component.gameObject.SetActive(false);
                component.transform.SetParent(_rootObject.transform);
                _instanceTracker.Remove(component.gameObject.GetInstanceID());
                _pool.Push(component);
                _currentInUse--;
            }

            public void TryReturn(Component component)
            {
                if (component is T typedComponent)
                    Return(typedComponent);
            }

            public void ClearObjects()
            {
                while (_pool.Count > 0)
                {
                    T component = _pool.Pop();
                    if (component != null)
                        Object.Destroy(component.gameObject);
                }
            }

            public void DestroyPool()
            {
                ClearObjects();
                _owner.UnregisterFromMonitor(_monitorKey);

                if (_rootObject != null)
                    Object.Destroy(_rootObject);
            }
        }
    }
}
