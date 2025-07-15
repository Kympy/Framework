using System.Threading;
using UnityEngine;

namespace Framework
{
    public abstract class EngineObject : MonoBehaviour, ICancelable
    {
        protected CancellationTokenSource _tokenSource;

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

        public CancellationTokenSource GetTokenSource()
        {
            _tokenSource ??= UniTaskHelper.CreateObjectToken(this);
            return _tokenSource;
        }

        public void CreateToken()
        {
            throw new System.NotImplementedException();
        }

        public void CancelToken()
        {
            UniTaskHelper.Cancel(_tokenSource);
        }
    }
}