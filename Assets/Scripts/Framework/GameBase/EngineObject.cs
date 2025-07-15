using UnityEngine;

namespace Framework
{
    public abstract class EngineObject : MonoBehaviour
    {
        private World _worldContext;
        
        public delegate void OnDestroyDelegate();
        public event OnDestroyDelegate DestroyCallback;

        public virtual void SetWorldContext(World worldContext)
        {
            _worldContext = worldContext;
        }

        public World GetWorld()
        {
            return _worldContext;
        }

        protected virtual void OnDestroy()
        {
            DestroyCallback?.Invoke();
        }
    }
}
