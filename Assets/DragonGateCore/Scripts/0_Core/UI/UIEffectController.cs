using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    public class UIEffectController
    {
        private readonly Dictionary<int, Canvas> _canvasByOrder = new Dictionary<int, Canvas>();
        private readonly Dictionary<string, PoolHandle<UIEffect>> _poolHandles = new();

        private List<UIEffect> _activeEffects = new();

        public void ShowEffect<T>(string resourceKey, Vector3 position, int sortOrderOverride = -1) where T : UIEffect
            => ShowEffect<T, int>(resourceKey, 0, position, sortOrderOverride);

        public void ShowEffect<T, V>(string resourceKey, V parameter, Vector3 position, int sortOrderOverride = -1) where T : UIEffect where V : struct
        {
            var effect = GetEffect<T>(resourceKey);
            if (effect is IViewState<V> viewState)
            {
                viewState.SetViewState(parameter);
            }
            _activeEffects.Add(effect);
            effect.Play();
        }

        private T GetEffect<T>(string resourceKey, int sortOrderOverride = -1) where T : UIEffect
        {
            if (_poolHandles.TryGetValue(resourceKey, out var poolHandle) == false)
            {
                poolHandle = PoolScope.CreatePool<UIEffect>(PoolScopeLoader.FromFunc(() => AssetManager.Instance.GetAsset<GameObject>(resourceKey)));
                _poolHandles.Add(resourceKey, poolHandle);
            }
            var effect = poolHandle.Get();
            int sortOrder = sortOrderOverride >= 0 ? sortOrderOverride : effect.SortOrder;
            var canvas = GetOrCreateCanvas(sortOrder);
            effect.transform.SetParent(canvas.transform, false);
            effect.Init();
            return effect as T;
        }

        public void ClearAllEffects()
        {
            foreach (var effect in _activeEffects)
            {
                PoolScope.Return(effect);
            }
            _activeEffects.Clear();
        }

        private Canvas GetOrCreateCanvas(int sortOrder)
        {
            if (_canvasByOrder.TryGetValue(sortOrder, out Canvas existingCanvas))
            {
                return existingCanvas;
            }

            Canvas newCanvas = UIManager.CreateCanvas(sortOrder, name: $"EffectCanvas_{sortOrder}");
            _canvasByOrder[sortOrder] = newCanvas;
            return newCanvas;
        }
    }
}