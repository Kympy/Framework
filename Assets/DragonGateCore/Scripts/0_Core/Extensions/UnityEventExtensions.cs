
using System;
using UnityEngine.Events;

namespace DragonGate
{
    public static class UnityEventExtensions
    {
        // public static void SetListener(this UnityEvent unityEvent, UnityAction action)
        // {
        //     unityEvent.RemoveAllListeners();
        //     unityEvent.AddListener(action);
        // }

        // 기존 이벤트 제거하고 = 대입 할당
        public static void SetListener(this UnityEvent unityEvent, Action action)
        {
            unityEvent.RemoveAllListeners();
            unityEvent.AddListener(action);
        }

        // 일회용 이벤트 할당
        public static void SetListenerOnce(this UnityEvent unityEvent, UnityAction action)
        {
            unityEvent.RemoveAllListeners();
            unityEvent.AddListenerOnce(action);
        }
        
        // 일회용 이벤트 추가
        public static void AddListenerOnce(this UnityEvent unityEvent, UnityAction action)
        {
            UnityAction wrapper = null;
            wrapper = () =>
            {
                action?.Invoke();
                unityEvent.RemoveListener(wrapper);
            };
            unityEvent.AddListener(wrapper);
        }

        public static void AddListener(this UnityEvent unityEvent, System.Action action)
        {
            unityEvent.AddListener(() => action?.Invoke());
        }
    }
}
