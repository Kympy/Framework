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
                AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
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
        
        public T GetAsset<T>(string key, Transform parent = null, bool worldPositionStays = true) where T : Object
        {
            T asset = LoadAsset<T>(key);
            if (asset == null)
            {
                return null;
            }
            if (asset is GameObject goAsset)
            {
                GameObject instance = Object.Instantiate(goAsset, parent, worldPositionStays);
                if (instance == null)
                    return null;
                _instanceCache.TryAdd(instance.GetInstanceID(), key);
                instance.AddComponent<AssetReleaseHelper>();
                return instance.GetComponent<T>();
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
    }
}
