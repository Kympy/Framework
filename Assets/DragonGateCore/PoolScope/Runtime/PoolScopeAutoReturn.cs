using System;
using System.Collections;
using UnityEngine;

namespace DragonGate
{
    /// <summary>
    /// Handles timed auto-return to the pool.
    ///
    /// Two ways to use:
    ///   1. Attach to prefab in Inspector, set Default Lifetime.
    ///      → pool.Get() will automatically return the object after that duration.
    ///   2. Call pool.Get(float lifetime) from code.
    ///      → Auto-attaches this component if not present, overrides defaultLifetime.
    ///
    /// The component is wired internally by PoolHandle. No manual setup needed beyond Inspector values.
    /// </summary>
    public sealed class PoolScopeAutoReturn : MonoBehaviour, IPoolable
    {
        [Tooltip("If greater than 0, the object auto-returns to the pool after this many seconds on every Get().\n" +
                 "Can be overridden at runtime by calling pool.Get(float lifetime).")]
        [SerializeField] private float _defaultLifetime = 0f;

        private Coroutine _autoReturnCoroutine;
        private Action _returnAction;

        public float DefaultLifetime => _defaultLifetime;

        // Called by PoolHandle.Get() after SetReturnAction, so _returnAction is guaranteed valid here.
        void IPoolable.OnGet()
        {
            if (_defaultLifetime > 0f)
                StartTimer(_defaultLifetime);
        }

        void IPoolable.OnReturn()
        {
            CancelTimer();
            _returnAction = null;
        }

        // Called by PoolHandle.Get() before OnGet() to wire the return delegate.
        internal void SetReturnAction(Action returnAction)
        {
            _returnAction = returnAction;
        }

        // Called by PoolHandle.Get(float lifetime) to override defaultLifetime.
        // Runs after OnGet(), so this cancels any timer OnGet started and restarts with the explicit value.
        internal void Activate(float lifetime, Action returnAction)
        {
            _returnAction = returnAction;
            CancelTimer();
            _autoReturnCoroutine = StartCoroutine(TimerCoroutine(lifetime));
        }

        private void StartTimer(float lifetime)
        {
            CancelTimer();
            _autoReturnCoroutine = StartCoroutine(TimerCoroutine(lifetime));
        }

        private void CancelTimer()
        {
            if (_autoReturnCoroutine != null)
            {
                StopCoroutine(_autoReturnCoroutine);
                _autoReturnCoroutine = null;
            }
        }

        private IEnumerator TimerCoroutine(float lifetime)
        {
            float elapsed = 0f;
            while (elapsed < lifetime)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
            _autoReturnCoroutine = null;
            _returnAction?.Invoke();
        }
    }
}
