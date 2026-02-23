using UnityEngine;

namespace DragonGate
{
    public static class ComponentExtensions
    {
        public static void SetActive(this Component component, bool value)
        {
            component.gameObject.SetActive(value);
        }

        public static T AddComponent<T>(this Component component) where T : Component
        {
            return component.gameObject.AddComponent<T>();
        }

        public static T AddComponent<T>(this Object obj) where T : Component
        {
            if (obj is GameObject gameObject)
            {
                return gameObject.AddComponent<T>();
            }
            if (obj is Component component)
            {
                return component.gameObject.AddComponent<T>();
            }
            return null;
        }

        public static T GetOrAddComponent<T>(this Component component) where T : Component
        {
            if (component.TryGetComponent(out T target))
            {
                return target;
            }
            return component.gameObject.AddComponent<T>();
        }

        public static T GetReference<T>(this T obj) where T : UnityEngine.Object
        {
            if (obj == null) return null;
            return obj;
        }
    }
}