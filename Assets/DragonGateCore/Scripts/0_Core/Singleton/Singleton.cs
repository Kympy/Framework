using UnityEngine;

namespace DragonGate
{
    public class Singleton<T> where T : Singleton<T>, new()
    {
        public static T Instance
        {
            get
            {
                lock (_lockObject)
                {
                    if (_instance == null)
                    {
                        return null;
                    }
                    return _instance;
                }
            }
        }
        
        public static bool HasInstance => _instance != null;

        private static T _instance = null;
        private static readonly object _lockObject = new(); 

        public static T CreateInstance()
        {
            lock (_lockObject)
            {
                if (_instance != null)
                {
                    throw new System.Exception($"Not allowed to create more than one instance of {typeof(T).Name}");
                }
                _instance = new T();
                _instance.OnCreate();
                return _instance;
            }
        }

        public static void DestroyInstance()
        {
            lock (_lockObject)
            {
                if (_instance == null) return;
                if (_instance is System.IDisposable disposable)
                {
                    disposable.Dispose();
                }
                _instance = null;
            }
        }

        protected virtual void OnCreate()
        {
            
        }
    }
}