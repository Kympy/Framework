using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    public partial class PoolManager
    {
        private interface IComponentPool
        {
            bool TryReturn(Component component);
            void Clear();
            int Count { get; }
        }

        private class ComponentPool<T> : IComponentPool where T : Component
        {
            public int Count => _pool.Count;

            private Stack<T> _pool = new();
            private string _resourceKey;
            private GameObject _rootObject;

            public ComponentPool(string resourceKey, Transform parentTransform = null)
            {
                _resourceKey = resourceKey;
                _rootObject = new GameObject($"{resourceKey}_{typeof(T)}");
                _rootObject.transform.SetParent(parentTransform);
            }

            public T Get(bool attachComponent = false)
            {
                T component;

                if (_pool.Count > 0)
                {
                    component = _pool.Pop();
                }
                else
                {
                    var gameObject = AssetManager.Instance.GetAsset<GameObject>(_resourceKey);
                    component = gameObject.GetComponent<T>();

                    if (component == null)
                    {
                        if (attachComponent)
                        {
                            component = gameObject.AddComponent<T>();
                        }
                        else
                        {
                            DGDebug.LogError($"Component {typeof(T).Name} not found on prefab {_resourceKey}");
                            return null;
                        }
                    }
                }
                
                component.gameObject.transform.SetParent(null);
                component.gameObject.SetActive(true);

                if (component is IPoolable poolable)
                    poolable.OnGet();

                return component;
            }

            public void Return(T component)
            {
                if (component == null) return;

                if (component is IPoolable poolable)
                    poolable.OnReturn();

                component.gameObject.SetActive(false);
                component.transform.SetParent(_rootObject.transform);
                _pool.Push(component);
            }

            public bool TryReturn(Component component)
            {
                if (GameStarter.IsApplicationQuitting) return false;
                if (component is T typedComponent)
                {
                    Return(typedComponent);
                    return true;
                }
                return false;
            }

            public void Clear()
            {
                while (_pool.Count > 0)
                {
                    var component = _pool.Pop();
                    if (component != null)
                        UnityEngine.Object.Destroy(component.gameObject);
                }
            }
        }
    }
}
