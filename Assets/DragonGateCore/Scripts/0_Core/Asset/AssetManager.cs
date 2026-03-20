using Cysharp.Threading.Tasks;
using System.Buffers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace DragonGate
{
    public partial class AssetManager : Singleton<AssetManager>
    {
        private Dictionary<AssetKey, AssetInfo> _assetCache = new Dictionary<AssetKey, AssetInfo>();
        private Dictionary<int, AssetKey> _instanceCache = new Dictionary<int, AssetKey>();

        /// <summary>
        /// Pre-loads an asset into the cache without instantiating it.
        /// ReferenceCount stays at 0 until GetAsset is called.
        /// Component and GameObject types are always loaded as GameObject internally.
        /// </summary>
        public async UniTask<bool> WarmUp<T>(AssetKey key) where T : Object
        {
            if (_assetCache.ContainsKey(key))
                return true;

            bool loadAsGameObject = typeof(Component).IsAssignableFrom(typeof(T)) || typeof(T) == typeof(GameObject);

            if (loadAsGameObject)
                return await LoadAssetAsync<GameObject>(key) != null;
            else
                return await LoadAssetAsync<T>(key) != null;
        }

        /// <summary>
        /// Returns the cached prefab without instantiating or incrementing ReferenceCount.
        /// Must be called after WarmUp.
        /// </summary>
        public GameObject PeekPrefab(AssetKey key)
        {
            if (_assetCache.TryGetValue(key, out AssetInfo info) && info is AssetInfo<GameObject> goInfo)
                return goInfo.Handle.Result;
            return null;
        }

        // Loads the asset into cache with ReferenceCount = 0. Does not instantiate.
        private T LoadAsset<T>(AssetKey key) where T : Object
        {
            if (_assetCache.TryGetValue(key, out AssetInfo assetInfo))
            {
                if (assetInfo is AssetInfo<T> typedAssetInfo == false)
                    throw new System.Exception($"Asset with key {key} is cached as a different type than {typeof(T)}.");
                return typedAssetInfo.Handle.Result;
            }

            var handle = Addressables.LoadAssetAsync<T>(key.Value);
            handle.WaitForCompletion();
            if (handle.Status == AsyncOperationStatus.Failed)
                return null;

            _assetCache.TryAdd(key, new AssetInfo<T> { Key = key, Handle = handle, ReferenceCount = 0 });
            return handle.Result;
        }

        private async UniTask<T> LoadAssetAsync<T>(AssetKey key) where T : Object
        {
            if (_assetCache.TryGetValue(key, out AssetInfo assetInfo))
            {
                if (assetInfo is AssetInfo<T> typedAssetInfo == false)
                    throw new System.Exception($"Asset with key {key} is cached as a different type than {typeof(T)}.");
                return typedAssetInfo.Handle.Result;
            }

            var handle = Addressables.LoadAssetAsync<T>(key.Value);
            await handle;
            if (handle.Status == AsyncOperationStatus.Failed)
                return null;

            _assetCache.TryAdd(key, new AssetInfo<T> { Key = key, Handle = handle, ReferenceCount = 0 });
            return handle.Result;
        }

        /// <summary>
        /// Instantiates and returns a GameObject or Component, or returns the raw asset for non-instantiable types.
        /// Increments ReferenceCount. Call ReleaseAsset when done.
        /// </summary>
        public T GetAsset<T>(AssetKey key, Transform parent = null, bool worldPositionStays = true) where T : Object
        {
            bool isComponent  = typeof(Component).IsAssignableFrom(typeof(T));
            bool isGameObject = typeof(T) == typeof(GameObject);

            if (isComponent || isGameObject)
            {
                var prefab = LoadAsset<GameObject>(key);
                if (prefab == null) return null;

                IncrementReferenceCount(key);

                var instance = Object.Instantiate(prefab, parent, worldPositionStays);
                instance.AddComponent<AssetReleaseHelper>();
                _instanceCache.TryAdd(instance.GetInstanceID(), key);

                if (isGameObject)
                    return instance as T;

                var comp = instance.GetComponent<T>();
                if (comp == null)
                {
                    DGDebug.LogError($"[{nameof(AssetManager)}] Component {typeof(T).Name} not found on instantiated GameObject for key {key}.");
                    return null;
                }
                return comp;
            }
            else
            {
                T asset = LoadAsset<T>(key);
                if (asset == null) return null;

                IncrementReferenceCount(key);
                _instanceCache.TryAdd(asset.GetInstanceID(), key);
                return asset;
            }
        }

        public async UniTask<T> GetAssetAsync<T>(AssetKey key, Transform parent = null, bool worldPositionStays = true) where T : Object
        {
            bool isComponent  = typeof(T).IsComponent();
            bool isGameObject = typeof(T).IsGameObject();

            if (isComponent || isGameObject)
            {
                var prefab = await LoadAssetAsync<GameObject>(key);
                if (prefab == null) return null;

                IncrementReferenceCount(key);

                var instance = Object.Instantiate(prefab, parent, worldPositionStays);
                instance.AddComponent<AssetReleaseHelper>();
                _instanceCache.TryAdd(instance.GetInstanceID(), key);

                if (isGameObject)
                    return instance as T;

                var comp = instance.GetComponent<T>();
                if (comp == null)
                {
                    DGDebug.LogError($"[{nameof(AssetManager)}] Component {typeof(T).Name} not found on instantiated GameObject for key {key}.");
                    return null;
                }
                return comp;
            }
            else
            {
                T asset = await LoadAssetAsync<T>(key);
                if (asset == null) return null;

                IncrementReferenceCount(key);
                _instanceCache.TryAdd(asset.GetInstanceID(), key);
                return asset;
            }
        }

        public void ReleaseAsset<T>(T asset) where T : Object
        {
            if (asset == null) return;

            int instanceId;
            if (asset.IsComponent())
                instanceId = (asset as Component).gameObject.GetInstanceID();
            else if (asset.IsGameObject())
                instanceId = (asset as GameObject).GetInstanceID();
            else
                instanceId = asset.GetInstanceID();

            if (_instanceCache.TryGetValue(instanceId, out AssetKey key) == false)
            {
                DGDebug.LogWarning($"Trying to release unmanaged asset {instanceId} {asset.name}");
                return;
            }

            if (_assetCache.TryGetValue(key, out AssetInfo assetInfo) == false)
            {
                DGDebug.LogError($"Asset with key {key} AssetInfo not found in cache.");
                return;
            }

            bool isUnityObject = asset.IsComponent() || asset.IsGameObject();
            assetInfo.ReferenceCount--;

            if (isUnityObject == false && assetInfo.ReferenceCount == 0)
                _instanceCache.Remove(instanceId);

            if (GameStarter.IsApplicationQuitting && assetInfo.ReferenceCount > 0)
                DGDebug.Log($"Asset released. {assetInfo.Key} : ReferenceCount: {assetInfo.ReferenceCount}", Color.red);
            else
                DGDebug.Log($"Asset released. {assetInfo.Key} : ReferenceCount: {assetInfo.ReferenceCount}");

            if (assetInfo.ReferenceCount <= 0)
            {
                assetInfo.UnloadAsset();
                _assetCache.Remove(key);
            }
        }

        public void ReleaseUnReferencedAssets()
        {
            var keysToRemove = ArrayPool<AssetKey>.Shared.Rent(_assetCache.Count);
            int index = 0;
            foreach (var asset in _assetCache)
            {
                if (asset.Value.ReferenceCount <= 0)
                {
                    asset.Value.UnloadAsset();
                    keysToRemove[index] = asset.Key;
                    index++;
                }
            }
            for (int i = 0; i < index; i++)
                _assetCache.Remove(keysToRemove[i]);
            ArrayPool<AssetKey>.Shared.Return(keysToRemove, clearArray: true);
        }

        public void NotifyUnReleasedAssets()
        {
            if (_assetCache.Count == 0) return;
            foreach (var assetInfo in _assetCache)
                DGDebug.LogWarning($"{assetInfo.Value.Key} Count : {assetInfo.Value.ReferenceCount}");
        }

        private void IncrementReferenceCount(AssetKey key)
        {
            if (_assetCache.TryGetValue(key, out AssetInfo assetInfo))
                assetInfo.ReferenceCount++;
        }
    }
}