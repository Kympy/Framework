using System.Buffers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Framework
{
    public class AssetManager : Singleton<AssetManager>
    {
        private Dictionary<string, AssetInfo> _assetCache = new Dictionary<string, AssetInfo>();
        private Dictionary<int, string> _instanceCache = new Dictionary<int, string>();

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
        
        /// <summary>
        /// Material, Sprite 등 사용했으면 반드시 Release 해주기.
        /// </summary>
        public T GetAsset<T>(string key, Transform parent = null, bool worldPositionStays = true) where T : Object
        {
            T asset = null;
            bool isComponent = typeof(Component).IsAssignableFrom(typeof(T)); 
            if (isComponent)
            {
                GameObject gameObjectAsset = LoadAsset<GameObject>(key);
                asset = gameObjectAsset.GetComponent<T>();
            }
            else
            {
                asset = LoadAsset<T>(key);
            }
            
            if (asset == null)
            {
                return null;
            }
            
            if (isComponent)
            {
                T instance = Object.Instantiate(asset, parent, worldPositionStays);
                _instanceCache.TryAdd(instance.GetInstanceID(), key);
                var component = instance as Component;
                component.gameObject.AddComponent<AssetReleaseHelper>();
                return instance;
            }
            else
            {
                // Material, Sprite, 기타 Object 타입은 그대로 반환
                _instanceCache.TryAdd(asset.GetInstanceID(), key);
                return asset;
            }
        }

        public void ReleaseAsset<T>(T asset) where T : Object
        {
            if (asset == null)
            {
                return;
            }

            int instanceId = asset.GetInstanceID();
            if (_instanceCache.TryGetValue(instanceId, out string key) == false)
            {
                Addressables.Release(asset);
                return;
            }
            _instanceCache.Remove(instanceId);
            
            if (_assetCache.TryGetValue(key, out AssetInfo assetInfo) == false)
            {
                return;
            }
            assetInfo.ReferenceCount--;
            DGLog.Log($"Asset released. {assetInfo.Key} : ReferenceCount: {assetInfo.ReferenceCount}");
            if (asset is Component componentAsset)
            {
                Object.Destroy(componentAsset.gameObject);
            }
            else
            {
                Object.Destroy(asset);
            }
            if (assetInfo.ReferenceCount <= 0)
            {
                Addressables.Release(asset);
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
            ArrayPool<string>.Shared.Return(keysToRemove);
        }

        private bool IsGameObjectType<T>()
        {
            bool isGameObject = typeof(T) != typeof(Material) && typeof(T) != typeof(Sprite) && typeof(T) != typeof(Texture2D) && typeof(T) != typeof(AudioClip) && typeof(T) != typeof(Texture) && typeof(T) != typeof(AnimationClip) && typeof(T) != typeof(Shader) && typeof(T) != typeof(TextAsset);
            return isGameObject;
        }
    }
}
