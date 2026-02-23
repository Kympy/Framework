using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    /// <summary>
    /// 런타임 블랙보드. 타입별 Dictionary로 박싱 없이 값을 저장한다.
    /// BlackboardAsset에서 코드 생성된 string 상수를 GetKey()에 전달해 키를 얻는다. -> 캐싱해놓고 쓰면 조회비용을 아낄 수 있긴한데, 사실 충분히 빠름.
    /// </summary>
    public sealed class Blackboard
    {
        private object _self;
        private readonly Dictionary<Type, object>  _typedStores = new();
        private readonly Dictionary<string, int>   _nameToId    = new();
        private int _nextId;

        public BlackboardAsset Asset { get; private set; }

        public void Initialize(BlackboardAsset asset, object self)
        {
            Asset = asset;
            _self = self;
            _typedStores.Clear();
            _nameToId.Clear();
            _nextId = 0;

            if (asset == null) return;

            foreach (var key in asset.keys)
                _nameToId[key.Name] = _nextId++;
        }
        // Brain을 가지는 자기 자신을 반환함. Blackboard 생성단계에서 무조건 self 인자를 받도록 되어있음.
        public T Self<T>() where T : class => _self as T;

        /// <summary>
        /// 키 이름으로 타입이 각인된 BlackboardKey를 반환한다.
        /// </summary>
        public BlackboardKey<T> GetKey<T>(string name)
        {
            if (!_nameToId.TryGetValue(name, out var id))
                throw new ArgumentException($"[Blackboard] 키를 찾을 수 없습니다: '{name}'");

            return new BlackboardKey<T>(id, GetStore<T>());
        }

        public BlackboardKey<object> GetObjectKey(string name) => GetKey<object>(name);
        public BlackboardKey<int> GetIntKey(string name) => GetKey<int>(name);
        public BlackboardKey<float> GetFloatKey(string name) => GetKey<float>(name);
        public BlackboardKey<long> GetLongKey(string name) => GetKey<long>(name);
        public BlackboardKey<bool> GetBoolKey(string name) => GetKey<bool>(name);
        public BlackboardKey<string> GetStringKey(string name) => GetKey<string>(name);
        public BlackboardKey<Vector3> GetVector3Key(string name) => GetKey<Vector3>(name);
        public BlackboardKey<Vector2> GetVector2Key(string name) => GetKey<Vector2>(name);

        public void Set<T>(BlackboardKey<T> key, T value) => key.Store[key.Id] = value;
        public void SetObject(BlackboardKey<object> key, object value) => Set<object>(key, value);
        public void SetInt(BlackboardKey<int> key, int value) => Set<int>(key, value);
        public void SetFloat(BlackboardKey<float> key, float value) => Set<float>(key, value);
        public void SetLong(BlackboardKey<long> key, long value) => Set<long>(key, value);
        public void SetBool(BlackboardKey<bool> key, bool value) => Set<bool>(key, value);
        public void SetString(BlackboardKey<string> key, string value) => Set<string>(key, value);
        public void SetVector3(BlackboardKey<Vector3> key, Vector3 value) => Set<Vector3>(key, value);
        public void SetVector2(BlackboardKey<Vector2> key, Vector2 value) => Set<Vector2>(key, value);
        
        public T Get<T>(BlackboardKey<T> key) => key.Store[key.Id];
        public object GetObject(BlackboardKey<object> key) => Get<object>(key);
        public int GetInt(BlackboardKey<int> key) => Get<int>(key);
        public float GetFloat(BlackboardKey<float> key) => Get<float>(key);
        public long GetLong(BlackboardKey<long> key) => Get<long>(key);
        public bool GetBool(BlackboardKey<bool> key) => Get<bool>(key);
        public string GetString(BlackboardKey<string> key) => Get<string>(key);
        public Vector3 GetVector3(BlackboardKey<Vector3> key) => Get<Vector3>(key);
        public Vector2 GetVector2(BlackboardKey<Vector2> key) => Get<Vector2>(key);

        // 편의성을 위해 string 키를 바로 받아, Blackboard Key로 변환 후 사용
        public T Get<T>(string keyString)
        {
            var key = GetKey<T>(keyString);
            return key.Store[key.Id];
        }
        public object GetObject(string keyString) => Get<object>(keyString);
        public int GetInt(string keyString) => Get<int>(keyString);
        public float GetFloat(string keyString) => Get<float>(keyString);
        public long GetLong(string keyString) => Get<long>(keyString);
        public bool GetBool(string keyString) => Get<bool>(keyString);
        public string GetString(string keyString) => Get<string>(keyString);
        public Vector3 GetVector3(string keyString) => Get<Vector3>(keyString);
        public Vector2 GetVector2(string keyString) => Get<Vector2>(keyString);

        public bool TryGet<T>(BlackboardKey<T> key, out T value) => key.Store.TryGetValue(key.Id, out value);
        public bool TryGetObject<T>(BlackboardKey<T> key, out T value) => TryGet<T>(key, out value);
        public bool TryGetInt<T>(BlackboardKey<T> key, out T value) => TryGet<T>(key, out value);
        public bool TryGetFloat<T>(BlackboardKey<T> key, out T value) => TryGet<T>(key, out value);
        public bool TryGetLong<T>(BlackboardKey<T> key, out T value) => TryGet<T>(key, out value);
        public bool TryGetBool<T>(BlackboardKey<T> key, out T value) => TryGet<T>(key, out value);
        public bool TryGetString<T>(BlackboardKey<T> key, out T value) => TryGet<T>(key, out value);
        public bool TryGetVector3<T>(BlackboardKey<T> key, out T value) => TryGet<T>(key, out value);
        public bool TryGetVector2<T>(BlackboardKey<T> key, out T value) => TryGet<T>(key, out value);

        public void Clear()
        {
            foreach (var store in _typedStores.Values)
                if (store is IDictionary dict) dict.Clear();
        }

        private Dictionary<int, T> GetStore<T>()
        {
            var type = typeof(T);
            if (_typedStores.TryGetValue(type, out var store))
                return (Dictionary<int, T>)store;

            var newStore = new Dictionary<int, T>();
            _typedStores[type] = newStore;
            return newStore;
        }
    }
}