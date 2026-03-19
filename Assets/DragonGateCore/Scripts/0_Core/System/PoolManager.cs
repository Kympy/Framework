using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DragonGate
{
    // public interface IPoolable
    // {
    //     void OnGet();
    //     void OnReturn();
    // }

    public partial class PoolManager : Singleton<PoolManager>, IDisposable
    {
        private Dictionary<Type, Stack<object>> _classPool = new();
        private Dictionary<long, IComponentPool> _componentPools = new();
        private Dictionary<int, long> _instanceToPoolKey = new();
        private GameObject _rootObject;

        protected override void OnCreate()
        {
            base.OnCreate();
            _rootObject = new GameObject(nameof(PoolManager));
            Object.DontDestroyOnLoad(_rootObject);
        }

        public void Dispose()
        {
            Object.Destroy(_rootObject);
            ClearAll();
        }

        public T GetComponent<T>(string resourceKey, bool attachComponent = false) where T : Component
        {
            long poolKey = HashCode.Combine(resourceKey.ToHash(), typeof(T).GetHashCode());

            if (!_componentPools.TryGetValue(poolKey, out var pool))
            {
                var newPool = new ComponentPool<T>(resourceKey, _rootObject.transform);
                _componentPools[poolKey] = newPool;
                pool = newPool;
            }

            var typedPool = (ComponentPool<T>)pool;
            var component = typedPool.Get(attachComponent);

            int instanceId = component.gameObject.GetInstanceID();
            _instanceToPoolKey[instanceId] = poolKey;
            return component;
        }

        public GameObject GetGameObject(string resourceKey)
        {
            return GetComponent<Transform>(resourceKey).gameObject;
        }

        public Fx GetFx(string resourceKey)
        {
            return GetComponent<Fx>(resourceKey, attachComponent: true);
        }

        public T GetFx<T>(string resourceKey) where T : Fx
        {
            return GetComponent<T>(resourceKey, attachComponent: true);
        }

        public void ReturnGameObject(GameObject gameObject)
        {
            if (gameObject == null) return;
            ReturnComponent(gameObject.transform);
        }

        public void ReturnComponent(Component component)
        {
            if (component == null) return;

            int instanceId = component.gameObject.GetInstanceID();
            if (!_instanceToPoolKey.TryGetValue(instanceId, out long poolKey))
            {
                UnityEngine.Debug.LogError($"Component {component.name} was not created from pool");
                Object.Destroy(component.gameObject);
                return;
            }

            if (_componentPools.TryGetValue(poolKey, out var pool) == false)
            {
                DGDebug.LogError($"Pool Key : {poolKey} did not exist.");
                Object.Destroy(component.gameObject);
                return;
            }
            pool.TryReturn(component);
        }

        public void ReturnFx(Fx fx)
        {
            if (fx == null) return;
            ReturnComponent(fx);
        }

        public T GetClass<T>() where T : class, new()
        {
            var type = typeof(T);
            if (!_classPool.TryGetValue(type, out var pool))
            {
                pool = new Stack<object>();
                _classPool[type] = pool;
            }

            T instance;
            if (pool.Count == 0)
            {
                instance = new T();
            }
            else
            {
                instance = (T)pool.Pop();
            }

            if (instance is IPoolable poolable)
                poolable.OnGet();

            return instance;
        }

        public void ReturnClass<T>(T instance) where T : class
        {
            if (instance == null) return;

            Type type = typeof(T);
            if (!_classPool.TryGetValue(type, out var pool))
            {
                pool = new Stack<object>();
                _classPool[type] = pool;
            }

            if (instance is IPoolable poolable)
                poolable.OnReturn();

            pool.Push(instance);
        }

        public void ClearClass<T>() where T : class
        {
            Type type = typeof(T);
            if (_classPool.TryGetValue(type, out var pool))
            {
                pool.Clear();
            }
        }

        public void ClearAll()
        {
            foreach (var pool in _componentPools.Values)
            {
                pool.Clear();
            }
            _componentPools.Clear();
            _instanceToPoolKey.Clear();

            foreach (var pool in _classPool.Values)
            {
                pool.Clear();
            }
        }
    }
}
