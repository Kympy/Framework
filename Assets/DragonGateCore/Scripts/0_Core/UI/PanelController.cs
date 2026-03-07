using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DragonGate
{
    public class PanelController
    {
        private enum ELoadStatus { None, Loading, Loaded }

        private class PanelEntry
        {
            public string Key;
            public ELoadStatus LoadStatus;
            public PanelCore Instance;
        }

        private Dictionary<string, PanelEntry> _entries = new();
        private PanelEntry _currentPanel;
        private Stack<PanelEntry> _history = new();
        private Canvas _panelCanvas;

        public PanelCore Show(string key) => Show(key, 0);
        public T Show<T>(string key) where T : PanelCore => Show(key) as T;
        public PanelCore Show(PanelCore instance) => Show(instance, 0);
        public T Show<T>(PanelCore instance) where T : PanelCore => Show(instance) as T;

        public PanelCore Show<ViewState>(string key, ViewState viewState) where ViewState : struct
        {
            var entry = GetOrCreateEntry(key);
            if (LoadPanel(entry) == false) return null;
            return ActivatePanel(entry, viewState);
        }

        public PanelCore Show<ViewState>(PanelCore instance, ViewState viewState) where ViewState : struct
        {
            var entry = GetOrCreateEntryByInstance(instance);
            return ActivatePanel(entry, viewState);
        }

        public T Show<T, ViewState>(string key, ViewState viewState) where ViewState : struct where T : PanelCore
        {
            return Show(key, viewState) as T;
        }

        public void Hide(PanelCore panel)
        {
            if (panel == null) return;
            if (_currentPanel?.Instance == panel)
            {
                _currentPanel = null;
            }
            panel.SetHidden();
        }

        public void Hide(string key)
        {
            if (_entries.TryGetValue(key, out var entry) && entry.LoadStatus == ELoadStatus.Loaded)
            {
                Hide(entry.Instance);
            }
        }

        public bool Back()
        {
            if (_history.Count == 0) return false;

            var previousPanel = _history.Pop();
            Show(previousPanel.Key, 0);
            return true;
        }

        public Canvas GetPanelCanvas()
        {
            if (_panelCanvas == null)
            {
                _panelCanvas = UIManager.CreateCanvas(UISortOrder.Panel, name: "Panel Canvas");
            }
            return _panelCanvas;
        }

        public void SetViewState<TViewState>(System.Type targetType, in TViewState viewState) where TViewState : struct
        {
            if (_currentPanel?.Instance?.GetType() == targetType && _currentPanel.Instance is IViewState<TViewState> stateView)
                stateView.SetViewState(in viewState);
        }

        public void SetVisible(bool visible)
        {
            if (_panelCanvas != null)
            {
                _panelCanvas.enabled = visible;
            }
        }

        private PanelEntry GetOrCreateEntry(string key)
        {
            if (_entries.TryGetValue(key, out var entry)) return entry;

            var newEntry = new PanelEntry { Key = key, LoadStatus = ELoadStatus.None };
            _entries.Add(key, newEntry);
            return newEntry;
        }

        private PanelEntry GetOrCreateEntryByInstance(PanelCore instance)
        {
            foreach (var e in _entries.Values)
                if (e.Instance == instance) return e;

            var key = instance.GetType().Name;
            var entry = new PanelEntry { Key = key, LoadStatus = ELoadStatus.Loaded, Instance = instance };
            _entries[key] = entry;
            instance.transform.SetParent(GetPanelCanvas().transform, false);
            return entry;
        }

        private PanelCore ActivatePanel<ViewState>(PanelEntry entry, ViewState viewState) where ViewState : struct
        {
            if (_currentPanel == entry) return entry.Instance;

            DGDebug.Log($"PanelController : Show ({entry.Key})", Color.yellow);

            if (_currentPanel != null)
            {
                _history.Push(_currentPanel);
                _currentPanel.Instance.SetHidden();
            }

            entry.Instance.SetVisible();
            _currentPanel = entry;

            if (entry.Instance is IViewState<ViewState> stateView)
                stateView.SetViewState(in viewState);

            return entry.Instance;
        }

        private bool LoadPanel(PanelEntry entry)
        {
            if (entry.LoadStatus == ELoadStatus.Loading) return false;
            if (entry.LoadStatus == ELoadStatus.Loaded) return true;

            entry.LoadStatus = ELoadStatus.Loading;

            var loadedAsset = AssetManager.Instance.GetAsset<GameObject>(entry.Key);
            if (loadedAsset == null)
            {
                DGDebug.LogError<PanelController>($"Load Asset Failed: {entry.Key}");
                entry.LoadStatus = ELoadStatus.None;
                return false;
            }

            var panelCore = loadedAsset.GetComponent<PanelCore>();
            if (panelCore == null)
            {
                DGDebug.LogError<PanelController>($"PanelCore component not found: {entry.Key}");
                entry.LoadStatus = ELoadStatus.None;
                return false;
            }

            entry.Instance = panelCore;
            entry.LoadStatus = ELoadStatus.Loaded;

            // 패널 캔버스 설정
            panelCore.transform.SetParent(GetPanelCanvas().transform, false);
            panelCore.Init();
            panelCore.SetActive(false);

            return true;
        }
    }
}