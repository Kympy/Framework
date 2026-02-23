using UnityEngine;

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

        #endregion
    }
}