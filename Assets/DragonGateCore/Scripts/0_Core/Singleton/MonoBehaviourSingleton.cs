using UnityEngine;

namespace DragonGate
{
    public class MonoBehaviourSingleton<T> : CoreBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; } = null;
        public static bool HasInstance => Instance != null;

        public static T CreateInstance()
        {
            return CreateInternal(false);
        }

        public static T CreateInstanceDontDestroyOnLoad()
        {
            return CreateInternal(true);
        }

        private static T CreateInternal(bool dontDestroyOnLoad)
        {
            if (Instance != null)
            {
                throw new System.Exception($"Not allowed to create more than one instance of {typeof(T).Name}");
            }
            var gameObject = new GameObject($"{typeof(T).Name}");
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
            var component = gameObject.AddComponent<T>();
            Instance = component;
            return Instance;
        }

        public static void DestroyInstance()
        {
            if (Instance == null) return;
            Destroy(Instance.gameObject);
            Instance = null;
        }

        protected virtual void Awake()
        {
            if (Instance != null)
            {
                throw new System.Exception($"Not allowed to create more than one instance of {typeof(T).Name}");
            }
            DGDebug.Log($"Singleton Mono Awake : {typeof(T)}", Color.aquamarine);
        }

        protected virtual void OnDestroy()
        {
            DGDebug.Log($"Singleton Mono Destroyed : {typeof(T)}", Color.chocolate);
        }
    }
}