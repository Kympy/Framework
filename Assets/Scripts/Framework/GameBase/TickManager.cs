using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    public class TickManager : MonoBehaviourSingleton<TickManager>
    {
        private Lazy<List<ITick>> _ticks = new Lazy<List<ITick>>(() => new List<ITick>());
        private Lazy<List<IFixedTick>> _fixedTicks = new Lazy<List<IFixedTick>>(() => new List<IFixedTick>());
        private Lazy<List<ILateTick>> _lateTicks = new Lazy<List<ILateTick>>(() => new List<ILateTick>());
        
        private bool _isTicking = false;
        
        private void Update()
        {
            if (_ticks.IsValueCreated == false) return;
            if (!_isTicking) return;
            float deltaTime = Time.deltaTime;
            int targetCount = _ticks.Value.Count;
            for (int i = 0; i < targetCount; i++)
            {
                _ticks.Value[i].Tick(deltaTime);
            }
        }
        
        private void FixedUpdate()
        {
            if (_fixedTicks.IsValueCreated == false) return;
            if (!_isTicking) return;
            float fixedDeltaTime = Time.fixedDeltaTime;
            int targetCount = _fixedTicks.Value.Count;
            for (int i = 0; i < targetCount; i++)
            {
                _fixedTicks.Value[i].FixedTick(fixedDeltaTime);
            }
        }
        
        private void LateUpdate()
        {
            if (_lateTicks.IsValueCreated == false) return;
            if (!_isTicking) return;
            float deltaTime = Time.deltaTime;
            int targetCount = _lateTicks.Value.Count;
            for (int i = 0; i < targetCount; i++)
            {
                _lateTicks.Value[i].LateTick(deltaTime);
            }
        }

        public void StartTick()
        {
            _isTicking = true;
        }

        public void StopTick()
        {
            _isTicking = false;
        }

        public void Register(ITick tick)
        {
            if (_ticks.Value.Contains(tick))
            {
                throw new InvalidOperationException($"Tick {tick} already registered.");
            }
            _ticks.Value.Add(tick);
        }
        public void Register(IFixedTick fixedTick)
        {
            if (_fixedTicks.Value.Contains(fixedTick))
            {
                throw new InvalidOperationException($"FixedTick {fixedTick} already registered.");
            }
            _fixedTicks.Value.Add(fixedTick);
        }
        public void Register(ILateTick lateTick)
        {
            if (_lateTicks.Value.Contains(lateTick))
            {
                throw new InvalidOperationException($"LateTick {lateTick} already registered.");
            }
            _lateTicks.Value.Add(lateTick);
        }
        public void Unregister(ITick tick)
        {
            if (_ticks.Value.Contains(tick))
            {
                _ticks.Value.Remove(tick);
            }
            else
            {
                throw new InvalidOperationException($"Tick {tick} not found.");
            }
        }
        public void Unregister(IFixedTick fixedTick)
        {
            if (_fixedTicks.Value.Contains(fixedTick))
            {
                _fixedTicks.Value.Remove(fixedTick);
            }
            else
            {
                throw new InvalidOperationException($"FixedTick {fixedTick} not found.");
            }
        }
        public void Unregister(ILateTick lateTick)
        {
            if (_lateTicks.Value.Contains(lateTick))
            {
                _lateTicks.Value.Remove(lateTick);
            }
            else
            {
                throw new InvalidOperationException($"LateTick {lateTick} not found.");
            }
        }
    }
}
