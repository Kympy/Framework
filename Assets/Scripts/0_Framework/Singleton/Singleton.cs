namespace Framework
{
    public class Singleton<T> where T : class, new()
    {
        public static T Instance => _instance;
        public static bool HasInstance => _instance != null;

        protected static T _instance;

        public static T CreateInstance()
        {
            if (_instance != null)
            {
                throw new System.Exception($"Instance {typeof(T)} already exists");
            }

            _instance = new T();
            return _instance;
        }

        public void DestroyInstance()
        {
            OnDestroy();
            _instance = null;
        }

        protected virtual void OnDestroy()
        {

        }
    }
}