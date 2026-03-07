using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DragonGate
{
    /// <summary>
    /// 게임 전체의 UI를 관리한다. PanelController와 PopupController를 orchestrate한다.
    /// Panel은 동시에 최대 1개만 존재한다는 룰이 전제된다.
    /// Popup은 여러 인스턴스가 동시에 존재할 수 있으며 풀링으로 관리된다.
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        private PanelController _panelController = new();
        private PopupController _popupController = new();
        public UIEffectController Effect { get; } = new();

        protected override void OnCreate()
        {
            base.OnCreate();
            _popupController.Init();
        }

        #region Panel

        public PanelCore ShowPanel(string key) => _panelController.Show(key);
        public T ShowPanel<T>(string key) where T : PanelCore => _panelController.Show<T>(key);
        public PanelCore ShowPanel<ViewState>(string key, ViewState viewState) where ViewState : struct
            => _panelController.Show(key, viewState);
        public T ShowPanel<T, ViewState>(string key, ViewState viewState) where ViewState : struct where T : PanelCore
            => _panelController.Show<T, ViewState>(key, viewState);

        public void HidePanel(string key) => _panelController.Hide(key);
        public void HidePanel(PanelCore panel) => _panelController.Hide(panel);

        #endregion

        #region Popup

        public PopupCore ShowPopup(string key) => _popupController.Show(key);
        public T ShowPopup<T>(string key) where T : PopupCore => _popupController.Show<T>(key);
        public PopupCore ShowPopup<ViewState>(string key, ViewState viewState) where ViewState : struct
            => _popupController.Show(key, viewState);
        public T ShowPopup<T, ViewState>(string key, ViewState viewState) where ViewState : struct where T : PopupCore
            => _popupController.Show<T, ViewState>(key, viewState);

        public void HidePopup(PopupCore popup) => _popupController.Hide(popup);
        public void HideAllPopup() => _popupController.HideAll();

        #endregion

        #region Common

        public void Show(UICore uiCore)
        {
            if (uiCore is PanelCore panelCore)
                _panelController.Show(panelCore);
            else if (uiCore is PopupCore popupCore)
                _popupController.Show(popupCore);
        }

        public void Hide(UICore uiCore)
        {
            if (uiCore == null) return;

            if (uiCore is PanelCore panelCore)
            {
                _panelController.Hide(panelCore);
            }
            else if (uiCore is PopupCore popupCore)
            {
                _popupController.Hide(popupCore);
            }
        }

        public void HideAll()
        {
            _popupController.HideAll();
            // 현재 패널도 숨기려면 추가 구현 필요
        }

        public void Back()
        {
            // 팝업 우선 닫기
            var topPopup = _popupController.GetTopPopup();
            if (topPopup != null)
            {
                _popupController.Hide(topPopup);
                return;
            }

            // 패널 히스토리 복귀
            _panelController.Back();
        }

        public void RefreshPanel<T>() where T : PanelCore => RefreshPanel<T, int>(0);

        public void RefreshPanel<TTarget, TViewState>(in TViewState viewState) where TTarget : PanelCore where TViewState : struct
        {
            var targetType = typeof(TTarget);
            _panelController.SetViewState<TViewState>(targetType, in viewState);
        }
        
        public void RefreshPopup<T>() where T : PopupCore => RefreshPopup<T, int>(0);

        public void RefreshPopup<TTarget, TViewState>(in TViewState viewState) where TTarget : PopupCore where TViewState : struct
        {
            var targetType = typeof(TTarget);
            _popupController.SetViewState<TViewState>(targetType, in viewState);
        }

        #endregion

        #region Util

        public void SetUIVisible(bool visible)
        {
            _panelController.SetVisible(visible);
        }

        public static Canvas CreateCanvas(int sortingOrder, RenderMode renderMode = RenderMode.ScreenSpaceOverlay, string name = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = "Canvas";
            }
            var canvas = new GameObject(name).AddComponent<Canvas>();
            canvas.renderMode = renderMode;
            canvas.sortingOrder = sortingOrder;

            canvas.AddComponent<GraphicRaycaster>();
            var scaler = canvas.AddComponent<CanvasScaler>();
            scaler.matchWidthOrHeight = 0;
            scaler.referenceResolution = new Vector2(1920, 1080);
                
            Object.DontDestroyOnLoad(canvas.gameObject);
            return canvas;
        }

        private UIFade _uiFade;
        private const string UIFadeKey = "UIFade";

        public async UniTask FromBlackToTransparent(float duration)
        {
            if (_uiFade == null)  CreateFade();
            await _uiFade.FromBlackToTransparent(duration);
        }

        public async UniTask FromTransparentToBlack(float duration)
        {
            if (_uiFade == null) CreateFade();
            await _uiFade.FromTransparentToBlack(duration);
        }
        
        public async UniTask FadeInOut(ICancelable cancelable, FadeData fadeData, float interval = 0f)
        {
            await FadeIn(fadeData);
            if (interval > 0f)
                await UniTaskHelper.WaitForSeconds(cancelable, interval);
            await FadeOut(fadeData);
        }

        public async UniTask FadeIn(FadeData fadeData)
        {
            if (_uiFade == null) CreateFade();
            _uiFade.SetStartColor(fadeData.InStartColor);
            _uiFade.SetEndColor(fadeData.InEndColor);
            await _uiFade.Play(fadeData.InDuration);
        }

        public async UniTask FadeOut(FadeData fadeData)
        {
            if (_uiFade == null) CreateFade();
            _uiFade.SetStartColor(fadeData.OutStartColor);
            _uiFade.SetEndColor(fadeData.OutEndColor);
            await _uiFade.Play(fadeData.OutDuration);
        }

        private void CreateFade()
        {
            if (_uiFade != null) return;
            _uiFade = AssetManager.Instance.GetAsset<UIFade>(UIFadeKey);
        }

        #endregion
    }
}