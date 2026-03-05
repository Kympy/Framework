using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    internal sealed class HandlerEntry
    {
        public IInputHandler Handler;
        public int Priority;
        public bool Enabled;
        public int InsertionOrder;

        public HandlerEntry(IInputHandler h, int prio, bool en, int order)
        {
            Handler = h;
            Priority = prio;
            Enabled = en;
            InsertionOrder = order;
        }
    }

    public partial class InputManager : Singleton<InputManager>, GameLoop.IGameUpdate, IDisposable
    {
        public bool IgnoreTimeScale { get; } = false;
    
        private readonly List<HandlerEntry> _handlers = new();
        private bool _dirty;
        private int _insertionCounter;

        protected override void OnCreate()
        {
            base.OnCreate();
            GameLoop.RegisterUpdate(this);
        }

        public void OnUpdate(float deltaTime)
        {
            if (_dirty)
            {
                // 우선순위 높은 순, 같으면 나중에 추가된 순 (LIFO)
                _handlers.Sort((a, b) =>
                {
                    int priorityCompare = b.Priority.CompareTo(a.Priority);
                    if (priorityCompare != 0) return priorityCompare;
                    return b.InsertionOrder.CompareTo(a.InsertionOrder);
                });
                _dirty = false;
            }
            int total = _handlers.Count;
            for (int i = 0; i < total; i++)
            {
                var handle = _handlers[i];
                if (!handle.Enabled) continue;  // Enabled 체크 추가!
                
                var result = handle.Handler.UpdateInput(deltaTime);
                if (result == EInputResult.Break)
                {
                    // DGLog.Log($"InputManager.OnUpdate: input result is break. {handle.Handler.GetType()}");
                    return;
                }
            }
        }

        public void Dispose()
        {
            GameLoop.UnregisterUpdate(this);
        }

        // 등록/해제/상태 제어
        public void Add(IInputHandler handler, int priority = 0, bool enabled = true)
        {
            if (handler == null) return;
            DGDebug.Log($"Add Input Handler : {handler.GetType()}", Color.aliceBlue);
            _handlers.Add(new HandlerEntry(handler, priority, enabled, _insertionCounter++));
            _dirty = true;
        }

        public void Remove(IInputHandler handler)
        {
            if (handler == null) return;
            DGDebug.Log($"Remove Input Handler : {handler.GetType()}", Color.aliceBlue);
            for (int i = _handlers.Count - 1; i >= 0; i--)
            {
                if (_handlers[i].Handler == handler)
                    _handlers.RemoveAt(i);
            }
        }

        public void SetEnabled(IInputHandler handler, bool enabled)
        {
            for (int i = 0; i < _handlers.Count; i++)
            {
                if (_handlers[i].Handler == handler)
                    _handlers[i].Enabled = enabled;
            }
        }

        public void SetPriority(IInputHandler handler, int priority)
        {
            for (int i = 0; i < _handlers.Count; i++)
            {
                if (_handlers[i].Handler == handler)
                {
                    _handlers[i].Priority = priority;
                    _dirty = true;
                }
            }
        }

        public static void AddInput(IInputHandler handler, int priority = 0, bool enabled = true)
        {
            Instance?.Add(handler, priority, enabled);
        }
        
        public static void RemoveInput(IInputHandler handler)
        {
            Instance?.Remove(handler);
        }
    }
}