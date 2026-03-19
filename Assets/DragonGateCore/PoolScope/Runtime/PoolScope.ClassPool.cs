using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    public partial class PoolScope
    {
        private int _classPoolSequence = 0;

        internal ClassPoolHandle<T> CreateClassPoolInternal<T>(int initialCount) where T : class, new()
        {
            _classPoolSequence++;
            string monitorKey = $"[Class] {typeof(T).Name} #{_classPoolSequence}";

            ClassPool<T> pool = new ClassPool<T>(monitorKey, initialCount, this);
            RegisterToMonitor(monitorKey, pool);
            return new ClassPoolHandle<T>(pool);
        }

        private void ClearAllClassPools()
        {
            // Handles own their pools; nothing to clear centrally.
        }

        // ----------------------------------------------------------------
        // ClassPool inner class
        // ----------------------------------------------------------------

        private sealed class ClassPool<T> : IClassPoolCore<T>, IPoolInfoProvider where T : class, new()
        {
            private readonly Stack<T> _pool = new Stack<T>();
            private readonly string _monitorKey;
            private readonly PoolScope _owner;

            private int _currentInUse = 0;
            private int _peakUsage = 0;
            private readonly Color _barColor = PoolScopeColor.GetNextColor();

            public string PoolName => _monitorKey;
            public int TotalCount => _pool.Count + _currentInUse;
            public int LeftInPool => _pool.Count;
            public int CurrentInUse => _currentInUse;
            public int PeakUsage => _peakUsage;
            public Color BarColor => _barColor;

            public ClassPool(string monitorKey, int initialCount, PoolScope owner)
            {
                _monitorKey = monitorKey;
                _owner = owner;

                for (int i = 0; i < initialCount; i++)
                    _pool.Push(new T());
            }

            public T Get()
            {
                T instance = _pool.Count > 0 ? _pool.Pop() : new T();

                if (instance is IPoolable poolable)
                    poolable.OnGet();

                _currentInUse++;
                if (_currentInUse > _peakUsage)
                    _peakUsage = _currentInUse;

                return instance;
            }

            public void Return(T instance)
            {
                if (instance == null) return;

                if (instance is IPoolable poolable)
                    poolable.OnReturn();

                _pool.Push(instance);
                _currentInUse--;
            }

            public void ClearObjects()
            {
                _pool.Clear();
            }

            public void DestroyPool()
            {
                _pool.Clear();
                _owner.UnregisterFromMonitor(_monitorKey);
            }
        }
    }
}
