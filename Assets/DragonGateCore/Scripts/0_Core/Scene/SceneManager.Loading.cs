using Cysharp.Threading.Tasks;
using UnityEngine.Events;

namespace DragonGate
{
    public partial class SceneManager
    {
        private UILoadingScreen _loadingScreen;
        public void ShowLoadingScreen(string loadingScreenKey, UnityAction onVisible = null)
        {
            // 로딩 스크린 정보가 없으면 즉시 진행
            if (string.IsNullOrEmpty(loadingScreenKey))
            {
                onVisible?.Invoke();
                return;
            }
            _loadingScreen = AssetManager.Instance.GetAsset<UILoadingScreen>(loadingScreenKey);
            _loadingScreen.SetVisible(onVisible);
        }

        public void HideLoadingScreen(UnityAction onHidden = null)
        {
            if (_loadingScreen == null)
            {
                onHidden?.Invoke();
                return;
            }
            _loadingScreen.SetHidden(onHidden);
        }
    }
}
