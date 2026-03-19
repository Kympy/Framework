using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DragonGate
{
    /// <summary>
    /// Main entry point for PoolScope. Auto-initializes before the first scene loads.
    /// No manual setup required.
    ///
    /// Usage:
    ///   PoolHandle&lt;Bullet&gt; pool = PoolScope.CreatePool&lt;Bullet&gt;(PoolScopeLoader.FromPrefab(prefab));
    ///   Bullet b = pool.Get();
    ///   pool.Return(b);
    /// </summary>
    public partial class PoolScope : MonoBehaviour
    {
        private static PoolScope _instance;
        private bool _isApplicationQuitting = false;

        public static PoolScope Instance
        {
            get
            {
                if (_instance == null) CreateInstance();
                return _instance;
            }
        }

        private readonly Dictionary<string, IPoolInfoProvider> _registeredPools = new Dictionary<string, IPoolInfoProvider>();
        public IReadOnlyDictionary<string, IPoolInfoProvider> RegisteredPools => _registeredPools;

        // ----------------------------------------------------------------
        // Static API - public entry points
        // ----------------------------------------------------------------

        /// <summary>
        /// Creates a new component pool and returns a handle to it.
        /// Calling this twice with the same type creates two independent pools.
        /// </summary>
        public static PoolHandle<T> CreatePool<T>(PoolScopeLoader loader, int initialCount = 0) where T : Component
        {
            return Instance.CreatePoolInternal<T>(loader, initialCount);
        }

        /// <summary>
        /// Creates a pool for plain GameObjects. Internally pools the Transform component.
        /// Use handle.GetObject() and handle.ReturnObject() for a GameObject-specific API.
        /// </summary>
        public static GameObjectPoolHandle CreateObjectPool(PoolScopeLoader loader, int initialCount = 0)
        {
            PoolHandle<Transform> innerHandle = Instance.CreatePoolInternal<Transform>(loader, initialCount);
            return new GameObjectPoolHandle(innerHandle);
        }

        /// <summary>
        /// Creates a new class object pool and returns a handle to it.
        /// </summary>
        public static ClassPoolHandle<T> CreateClassPool<T>(int initialCount = 0) where T : class, new()
        {
            return Instance.CreateClassPoolInternal<T>(initialCount);
        }

        /// <summary>
        /// Returns a component to its pool without needing the handle.
        /// The component must have been obtained via a PoolHandle.
        /// </summary>
        public static void Return(Component component)
        {
            Instance.ReturnComponentInternal(component);
        }

        /// <summary>
        /// Returns a GameObject to its pool without needing the handle.
        /// The object must have been obtained via a PoolHandle.
        /// </summary>
        public static void Return(GameObject pooledObject)
        {
            if (pooledObject == null) return;
            Instance.ReturnComponentInternal(pooledObject.transform);
        }

        // ----------------------------------------------------------------
        // Lifecycle
        // ----------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (_instance != null) return;
            CreateInstance();
        }

        private static void CreateInstance()
        {
            GameObject go = new GameObject("[PoolScope]");
            Object.DontDestroyOnLoad(go);
            _instance = go.AddComponent<PoolScope>();
            go.AddComponent<PoolScopeMonitor>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            Object.DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationQuit()
        {
            _isApplicationQuitting = true;
        }

        private void OnDestroy()
        {
            if (_isApplicationQuitting) return;
            _instance = null;
        }

        // ----------------------------------------------------------------
        // Monitor registration (used by pool internals)
        // ----------------------------------------------------------------

        internal void RegisterToMonitor(string key, IPoolInfoProvider provider)
        {
            if (_registeredPools.ContainsKey(key) == false)
                _registeredPools.Add(key, provider);
        }

        internal void UnregisterFromMonitor(string key)
        {
            _registeredPools.Remove(key);
        }

        internal bool IsApplicationQuitting => _isApplicationQuitting;
    }
}
