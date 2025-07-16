using Framework.Extensions;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Framework
{
    public class PopupContainer : EngineObject
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private CanvasScaler _scaler;
        [SerializeField] private ContainerBackground _transparentBackground;
        [SerializeField] private ContainerBackground _dimmedBackground;
        
        private PopupBase _popup;
        private ContainerBackground _currentBackground;
        private bool _allowBackgroundHideAction = true;

        private void Awake()
        {
            _scaler.SetCanvasMatchWidthOrHeight();
            GameUtil.SetActiveSafe(_dimmedBackground, true);
        }

        public void SetPopup(PopupBase popup)
        {
            if (_popup != null)
            {
                Object.Destroy(_popup.gameObject);
            }
            _popup = popup;
            if (_popup == null) return;
            _popup.transform.SetParent(transform, false);
        }

        public void SetOrder(int order)
        {
            _canvas.sortingOrder = order;
        }

        public void SetOption(PopupOption option)
        {
            SetBackgroundType(option.BackgroundType);
            _currentBackground.AllowBackgroundHide = option.AllowBackgroundHideAction;
        }

        private void SetBackgroundType(EPopupBackgroundType type)
        {
            GameUtil.SetActiveSafe(_dimmedBackground, _transparentBackground, false);
            _currentBackground = null;
            switch (type)
            {
                case EPopupBackgroundType.Dimmed:
                {
                    _currentBackground = _dimmedBackground;
                    GameUtil.SetActiveSafe(_dimmedBackground, true);
                    break;
                }
                case EPopupBackgroundType.Transparent:
                {
                    _currentBackground = _transparentBackground;
                    GameUtil.SetActiveSafe(_transparentBackground, true);
                    break;
                }
            }
        }
    }
}