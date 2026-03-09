using System;
using System.Threading;
using UnityEngine;

namespace DragonGate
{
    public class CoreBehaviour : MonoBehaviour, ICancelable
    {
        public new Transform transform
        {
            get
            {
                if (_transform == null)
                    _transform = base.transform;
                return _transform;
            }
        }

        public new GameObject gameObject
        {
            get
            {
                if (_gameObject == null)
                    _gameObject = base.gameObject;
                return _gameObject;
            }
        }
        
        private Transform _transform;
        private GameObject _gameObject;
        private CancellationTokenSource _tokenSource;

        public virtual void Init()
        {
            
        }

        protected virtual void OnDisable()
        {
            CancelToken();
        }

        public CancellationTokenSource GetTokenSource()
        {
            if (_tokenSource == null || _tokenSource.IsCancellationRequested)
            {
                _tokenSource = UniTaskHelper.CreateObjectToken(this);
            }
            return _tokenSource; 
        }

        public void CancelToken()
        {
            _tokenSource?.Cancel();
            _tokenSource?.Dispose();
            _tokenSource = null;
        }
        
        public bool IsValidCancelToken()
        {
            return _tokenSource is { IsCancellationRequested: false };
        }
    }
}