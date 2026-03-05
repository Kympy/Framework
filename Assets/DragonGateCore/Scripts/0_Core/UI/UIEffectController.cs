using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    public class UIEffectController
    {
        private Dictionary<int, Canvas> _canvasByOrder = new Dictionary<int, Canvas>();

        public void ShowEffect<T>(string resourceKey, Vector3 position, int sortOrderOverride = -1) where T : UIEffect
            => ShowEffect<T, int>(resourceKey, 0, position, sortOrderOverride);

        public void ShowEffect<T, V>(string resourceKey, V parameter, Vector3 position, int sortOrderOverride = -1) where T : UIEffect where V : struct
        {
            var effect = PoolManager.Instance.GetComponent<T>(resourceKey);
            int sortOrder = sortOrderOverride >= 0 ? sortOrderOverride : effect.SortOrder;
            var canvas = GetOrCreateCanvas(sortOrder);
            effect.transform.SetParent(canvas.transform, false);
            effect.transform.position = position;
            effect.Init();
            if (effect is IViewState<V> viewState)
            {
                viewState.SetViewState(parameter);
            }
            effect.Play();
        }

        public T GetEffect<T>(string resourceKey, int sortOrderOverride = -1) where T : UIEffect
        {
            var effect = PoolManager.Instance.GetComponent<T>(resourceKey);
            int sortOrder = sortOrderOverride >= 0 ? sortOrderOverride : effect.SortOrder;
            var canvas = GetOrCreateCanvas(sortOrder);
            effect.transform.SetParent(canvas.transform, false);
            effect.Init();
            return effect;
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