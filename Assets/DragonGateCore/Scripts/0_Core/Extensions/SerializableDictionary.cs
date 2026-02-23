using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    [System.Serializable]
    public class SerializableKeyValue<K,V>
    {
        public K Key;
        public V Value;
    }

    [System.Serializable]
    public class SerializableDictionary <K,V> : Dictionary<K,V>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<SerializableKeyValue<K, V>> _keyValueList;

        public void OnBeforeSerialize()
        {
            if (_keyValueList == null) return;
            _keyValueList.Clear();
        
            foreach (var kv in this)
            {
                _keyValueList.Add(new SerializableKeyValue<K, V>()
                {
                    Key = kv.Key,
                    Value = kv.Value
                });
            }        
        }

        public void OnAfterDeserialize() 
        {
            this.Clear();
            foreach (var kv in _keyValueList)
            {
                if(!this.TryAdd(kv.Key, kv.Value))
                {
                    // Debug.LogError($"List has duplicate Key");
                }
            }
        }
    }
}