using UnityEngine;

namespace Framework
{
    public class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance => _instance;

        private static T _instance;

        public static T CreateInstance(bool dontDestroyOnLoad = false)
        {
            if (_instance != null)
            {
                throw new System.Exception($"Instance {typeof(T)} already exists");
            }

            var obj = new GameObject();
#if UNITY_EDITOR
            obj.name = typeof(T).Name;
#endif
            _instance = obj.AddComponent<T>();

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(obj);
            }

            return _instance;
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                throw new System.Exception($"Instance {typeof(T)} already exists");
            }
            _instance = this as T;
        }

        protected virtual void OnDestroy()
        {
            _instance = null;
        }

        public virtual void DestroyInstance()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }
    }
}