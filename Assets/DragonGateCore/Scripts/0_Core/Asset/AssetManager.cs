using Cysharp.Threading.Tasks;
using System;
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
        private Dictionary<string, AssetInfo> _assetCache = new Dictionary<string, AssetInfo>();
        private Dictionary<int, string> _instanceCache = new Dictionary<int, string>();

        public async UniTask<bool> WarmUp<T>(string key) where T : Object
        {
            if (_assetCache.TryGetValue(key, out AssetInfo assetInfo))
            {
                return true;
            }
            var handle = Addressables.LoadAssetAsync<T>(key);
            await handle;
            if (handle.Status == AsyncOperationStatus.Failed)
            {
                return false;
            }
            AssetInfo<T> newAssetInfo = new AssetInfo<T>
            {
                Key = key,
                Handle = handle,
                ReferenceCount = 0,
            };
            _assetCache.TryAdd(key, newAssetInfo);
            return true;
        }

        private T LoadAsset<T>(string key) where T : Object
        {
            if (_assetCache.TryGetValue(key, out AssetInfo assetInfo) == false)
            {
                var handle = Addressables.LoadAssetAsync<T>(key); 
                handle.WaitForCompletion();
                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    return null;
                }
                AssetInfo<T> newAssetInfo = new AssetInfo<T>
                {
                    Key = key,
                    Handle = handle,
                    ReferenceCount = 1,
                };
                _assetCache.TryAdd(key, newAssetInfo);
                return handle.Result;
            }

            if (assetInfo is AssetInfo<T> typedAssetInfo == false)
            {
                throw new System.Exception($"Asset with key {key} is not of type {typeof(T)}");
            }
            typedAssetInfo.ReferenceCount++;
            return typedAssetInfo.Handle.Result;
        }

        private async UniTask<T> LoadAssetAsync<T>(string key) where T : Object
        {
            if (_assetCache.TryGetValue(key, out AssetInfo assetInfo) == false)
            {
                var handle = Addressables.LoadAssetAsync<T>(key);
                await handle;
                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    return null;
                }
                AssetInfo<T> newAssetInfo = new AssetInfo<T>
                {
                    Key = key,
                    Handle = handle,
                    ReferenceCount = 1,
                };
                _assetCache.TryAdd(key, newAssetInfo);
                return handle.Result;
            }

            if (assetInfo is AssetInfo<T> typedAssetInfo == false)
            {
                throw new System.Exception($"Asset with key {key} is not of type {typeof(T)}");
            }
            typedAssetInfo.ReferenceCount++;
            return typedAssetInfo.Handle.Result;
        }
        
        /// <summary>
        /// 생성해서 반환. Material, Sprite 등 사용했으면 반드시 Release 해주기.
        /// </summary>
        public T GetAsset<T>(string key, Transform parent = null, bool worldPositionStays = true) where T : Object
        {
            bool isComponent = typeof(Component).IsAssignableFrom(typeof(T));
            bool isGameObject = typeof(T) == typeof(GameObject);

            int instanceId;
            if (isComponent || isGameObject)
            {
                // Always instantiate the prefab GameObject, then get the component if requested
                var prefabGO = LoadAsset<GameObject>(key);
                if (prefabGO == null) return null;

                var goInstance = Object.Instantiate(prefabGO, parent, worldPositionStays);
                goInstance.AddComponent<AssetReleaseHelper>();

                instanceId = goInstance.GetInstanceID();
                _instanceCache.TryAdd(instanceId, key);

                if (isGameObject)
                    return goInstance as T;

                var comp = goInstance.GetComponent<T>();
                if (comp == null)
                {
                    DGDebug.LogError($"[{nameof(AssetManager)}] Component {typeof(T).Name} not found on instantiated GameObject for key {key}.");
                    return null;
                }
                return comp;
            }
            else
            {
                // Non-instantiable assets (Material, Sprite, etc.)
                T asset = LoadAsset<T>(key);
                if (asset == null) return null;
                instanceId = asset.GetInstanceID();
                _instanceCache.TryAdd(instanceId, key);
                return asset;
            }
        }

        public async UniTask<T> GetAssetAsync<T>(string key, Transform parent = null, bool worldPositionStays = true) where T : Object
        {
            bool isComponent = typeof(T).IsComponent();
            bool isGameObject = typeof(T).IsGameObject();

            int instanceId;
            if (isComponent || isGameObject)
            {
                var prefabGO = await LoadAssetAsync<GameObject>(key);
                if (prefabGO == null) return null;

                var goInstance = Object.Instantiate(prefabGO, parent, worldPositionStays);
                goInstance.AddComponent<AssetReleaseHelper>();

                instanceId = goInstance.GetInstanceID();
                _instanceCache.TryAdd(instanceId, key);

                if (isGameObject)
                    return goInstance as T;

                var comp = goInstance.GetComponent<T>();
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
                instanceId = asset.GetInstanceID();
                _instanceCache.TryAdd(instanceId, key);
                return asset;
            }
        }

        public void ReleaseAsset<T>(T asset) where T : Object
        {
            if (asset == null)
            {
                return;
            }

            // Normalize to GameObject instance id if the asset is a Component or GameObject
            int instanceId;
            if (asset.IsComponent())
            {
                var c = asset as Component;
                instanceId = c.gameObject.GetInstanceID();
            }
            else if (asset.IsGameObject())
            {
                instanceId = (asset as GameObject).GetInstanceID();
            }
            else
            {
                instanceId = asset.GetInstanceID();
            }

            if (_instanceCache.TryGetValue(instanceId, out string key) == false)
            {
                string msg = $"Trying to release unmanaged asset {instanceId} {asset.name}";
                if (asset.IsComponent())
                {
                    var c = asset as Component;
                    if (c != null)
                    {
                        msg += $", Component's GameObject InstanceID: {c.gameObject.GetInstanceID()}";
                    }
                }
                return;
            }
            
            bool isUnityObject = asset.IsComponent() || asset.IsGameObject();
            // We cache the GameObject instance ID for instantiated prefabs to avoid mismatches
            // when releasing either the Component or the GameObject itself.

            if (isUnityObject)
            {
                // Do not remove from _instanceCache here; removal happens when ref count hits zero.
            }
            
            if (_assetCache.TryGetValue(key, out AssetInfo assetInfo) == false)
            {
                DGDebug.LogError($"Asset with key {key} 'AssetInfo' is not exist on assetCache.");
                return;
            }
            assetInfo.ReferenceCount--;
            // 일반 객체에 대해서 Ref 가 0일 때, 캐시에서 제거
            if (isUnityObject == false && assetInfo.ReferenceCount == 0)
            {
                _instanceCache.Remove(instanceId);
            }
            DGDebug.Log($"Asset released. {assetInfo.Key} : ReferenceCount: {assetInfo.ReferenceCount}");
            if (assetInfo.ReferenceCount <= 0)
            {
                assetInfo.UnloadAsset();
                _assetCache.Remove(key);
            }
        }
        
        public void ReleaseUnReferencedAssets()
        {
            var keysToRemove = ArrayPool<string>.Shared.Rent(_assetCache.Count);
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
            {
                _assetCache.Remove(keysToRemove[i]);
            }
            ArrayPool<string>.Shared.Return(keysToRemove, true);
        }

        public void NotifyUnReleasedAssets()
        {
            if (_assetCache.Count == 0) return;

            foreach (var assetInfo in _assetCache)
            {
                DGDebug.LogWarning($"{assetInfo.Value.Key} Count : {assetInfo.Value.ReferenceCount}");
            }
        }
    }
}
