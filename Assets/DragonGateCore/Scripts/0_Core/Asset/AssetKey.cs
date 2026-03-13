using UnityEngine.AddressableAssets;

namespace DragonGate
{
    /// <summary>
    /// string 키와 AssetReference.RuntimeKey를 GC 없이 통합하는 키 타입.
    /// string에서 암묵적 변환을 지원하므로 기존 string 기반 호출부는 변경 불필요.
    /// </summary>
    public readonly struct AssetKey : System.IEquatable<AssetKey>
    {
        private readonly object _key;

        public AssetKey(string key)
        {
            _key = key;
        }

        /// <summary>
        /// AssetReference.RuntimeKey 등 object 키를 ToString() 없이 직접 저장.
        /// </summary>
        public AssetKey(object runtimeKey)
        {
            _key = runtimeKey;
        }

        /// <summary>
        /// string → AssetKey 암묵적 변환. 기존 string 기반 호출부 변경 불필요.
        /// </summary>
        public static implicit operator AssetKey(string key) => new AssetKey(key);

        /// <summary>
        /// AssetReference → AssetKey 암묵적 변환. RuntimeKey를 ToString() 없이 사용.
        /// </summary>
        public static implicit operator AssetKey(AssetReference assetReference) => new AssetKey(assetReference.RuntimeKey);

        public object Value => _key;

        public bool Equals(AssetKey other)
        {
            if (_key == null)
            {
                return other._key == null;
            }
            return _key.Equals(other._key);
        }

        public override bool Equals(object obj)
        {
            return obj is AssetKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _key?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// 디버그 출력용. 런타임 핫패스에서 호출 금지 (string 할당 발생).
        /// </summary>
        public override string ToString()
        {
            return _key?.ToString() ?? string.Empty;
        }
    }
}