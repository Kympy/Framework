using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DragonGate
{
    /// <summary>
    /// Defines how a ComponentPool creates new GameObjects.
    /// Use one of the static factory methods.
    /// </summary>
    public sealed class PoolScopeLoader
    {
        private readonly Func<GameObject> _instantiateFunc;

        private PoolScopeLoader(Func<GameObject> instantiateFunc)
        {
            _instantiateFunc = instantiateFunc;
        }

        /// <summary>
        /// Uses a prefab reference directly.
        /// </summary>
        public static PoolScopeLoader FromPrefab(GameObject prefab)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab), "[PoolScope] Prefab cannot be null.");

            return new PoolScopeLoader(() => Object.Instantiate(prefab));
        }

        /// <summary>
        /// Loads from Resources/ on demand. Path is relative to Resources/.
        /// e.g. "Prefabs/EnemyBullet"
        /// </summary>
        public static PoolScopeLoader FromResources(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
                throw new ArgumentNullException(nameof(resourcePath), "[PoolScope] Resource path cannot be null or empty.");

            return new PoolScopeLoader(() =>
            {
                GameObject prefab = Resources.Load<GameObject>(resourcePath);
                if (prefab == null)
                    throw new Exception($"[PoolScope] Resources.Load failed. Path: \"{resourcePath}\"");

                return Object.Instantiate(prefab);
            });
        }

        /// <summary>
        /// Uses a custom factory function. Suitable for Addressables or any external loader.
        /// The function must return a fully instantiated GameObject (not just an asset reference).
        ///
        /// Addressables example:
        ///   var prefab = await Addressables.LoadAssetAsync&lt;GameObject&gt;(key).Task;
        ///   PoolScopeLoader.FromFunc(() => Object.Instantiate(prefab));
        /// </summary>
        public static PoolScopeLoader FromFunc(Func<GameObject> instantiateFunc)
        {
            if (instantiateFunc == null)
                throw new ArgumentNullException(nameof(instantiateFunc), "[PoolScope] Instantiate function cannot be null.");

            return new PoolScopeLoader(instantiateFunc);
        }

        internal GameObject Instantiate()
        {
            return _instantiateFunc.Invoke();
        }
    }
}
