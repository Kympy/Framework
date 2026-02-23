using UnityEngine;

namespace DragonGate
{
    // 씬에 배치된 채로 사용되는 모노비헤이비어 싱글톤
    public class PlacedMonoBehaviourSingleton<T> : CoreBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; } = null;
        public static bool HasInstance => Instance != null;

        [SerializeField] private bool _dontDestroyOnLoad = false;
        
        protected virtual void Awake()
        {
            if (Instance != null)
            {
                throw new System.Exception($"Not allowed to create more than one instance of {typeof(T).Name}");
            }
            Instance = this as T;
            if (_dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
            DGDebug.Log($"Singleton Mono Awake : {typeof(T)}", Color.aquamarine);
        }

        protected virtual void OnDestroy()
        {
            Instance = null;
            DGDebug.Log($"Singleton Mono Destroyed : {typeof(T)}", Color.chocolate);
        }
    }
}
