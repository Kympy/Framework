using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Framework
{
    public class HUDBase : MonoBehaviour
    {
        [SerializeField] protected Canvas _mainCanvas;
        [SerializeField] protected Camera _uiCamera;

        protected LinkedList<PanelBase> _activePanels = new LinkedList<PanelBase>();
        protected Dictionary<Type, PanelBase> _cachedPanels = new Dictionary<Type, PanelBase>();

        public virtual void InitHUD()
        {
            _uiCamera.orthographic = true;
            if (_mainCanvas.TryGetComponent(out CanvasScaler canvasScaler))
            {
                _uiCamera.orthographicSize = canvasScaler.referenceResolution.y * 0.5f;
            }
        }

        private T CreatePanel<T>() where T : PanelBase
        {
            PanelBase panel = null;
            panel = AssetManager.Instance.GetAsset<T>(UIAsset.GetKey<T>());
            panel.InitPanel();
            panel.gameObject.SetActive(false);
            return panel as T;
        }

        public T ShowPanel<T>(IPanelParameter parameter = null) where T : PanelBase
        {
            HideTopPanel();
            if (_cachedPanels.TryGetValue(typeof(T), out PanelBase panel) == false)
            {
                panel = CreatePanel<T>();
            }
            else
            {
                panel = _cachedPanels[typeof(T)];
            }
            _activePanels.AddLast(panel);
            panel.BeforeShow(parameter);
            panel.gameObject.SetActive(true);
            return panel as T;
        }

        public void HideTopPanel()
        {
            var lastNode = _activePanels.Last;
            if (lastNode == null || lastNode.Value == null)
            {
                return;
            }
            lastNode.Value.gameObject.SetActive(false);
            _activePanels.RemoveLast();
            if (lastNode.Value.UseCaching)
            {
                _cachedPanels.TryAdd(lastNode.Value.GetType(), lastNode.Value);
            }
            else
            {
                Destroy(lastNode.Value.gameObject);
            }
        }
    }
}
