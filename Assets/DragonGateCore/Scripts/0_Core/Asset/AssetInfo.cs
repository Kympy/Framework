using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DragonGate
{
    public abstract class AssetInfo
    {
        public string Key;
        public int ReferenceCount = 1;

        public abstract void UnloadAsset();
    }

    public class AssetInfo<T> : AssetInfo where T : UnityEngine.Object
    {
        public AsyncOperationHandle<T> Handle;
        public override void UnloadAsset()
        {
            Addressables.Release(Handle);
            ReferenceCount = 0;
        }
    }
}
