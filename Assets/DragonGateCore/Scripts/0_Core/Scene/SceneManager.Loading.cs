using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DragonGate
{
    public partial class SceneManager
    {
        private Dictionary<string, UILoadingScreen> _loadingScreens = new();
        private UILoadingScreen _currentLoadingScreen;
        private Canvas _loadingCanvas;
        
        public void ShowLoadingScreen(string loadingScreenKey, UnityAction onVisible = null)
        {
            // 로딩 스크린 정보가 없으면 즉시 진행
            if (string.IsNullOrEmpty(loadingScreenKey))
            {
                onVisible?.Invoke();
                return;
            }

            if (_loadingScreens.TryGetValue(loadingScreenKey, out var loadingScreen))
            {
                _currentLoadingScreen = loadingScreen;
                loadingScreen.SetVisible(onVisible);
                return;
            }
            
            if (_loadingCanvas == null)
                _loadingCanvas = UIManager.CreateCanvas(UISortOrder.Loading, name: "LoadingCanvas");
            
            var newLoadingScreen = AssetManager.Instance.GetAsset<UILoadingScreen>(loadingScreenKey);
            if (newLoadingScreen == null)
            {
                return;
            }
            newLoadingScreen.transform.SetParent(_loadingCanvas.transform, false);
            newLoadingScreen.transform.ResetLocal(false);
            
            _loadingScreens[loadingScreenKey] = newLoadingScreen;
            _currentLoadingScreen = newLoadingScreen;
            newLoadingScreen.SetVisible(onVisible);
        }

        public void HideLoadingScreen(UnityAction onHidden = null)
        {
            if (_currentLoadingScreen == null)
            {
                onHidden?.Invoke();
                return;
            }
            _currentLoadingScreen.SetHidden(onHidden);
        }

        public void SetLoadingProgress(float progress)
        {
            if (_currentLoadingScreen == null) return;
            _currentLoadingScreen.SetProgress(progress);
        }

        public bool IsLoadingProgressComplete()
        {
            if (_currentLoadingScreen == null) return true;
            return _currentLoadingScreen.IsProgressComplete;
        }
    }
}
