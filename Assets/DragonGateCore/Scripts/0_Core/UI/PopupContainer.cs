using UnityEngine;
using UnityEngine.UI;

namespace DragonGate
{
    [RequireComponent(typeof(Canvas))]
    public class PopupContainer : CoreBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Image _background;
        
        private PopupCore _popup;
        private GameObject _containerRoot;
        private float _defaultAlpha;

        public void Init(GameObject containerRoot)
        {
            _containerRoot = containerRoot;
            _defaultAlpha = _background.color.a;
        }
        
        public void SetPopup(PopupCore newPopup)
        {
            if (newPopup == null)
            {
                if (_popup != null)
                {
                    _popup.SetContainer(null);
                    _popup.transform.SetParent(_containerRoot.transform);
                }

                gameObject.name = nameof(PopupContainer);
                _popup = null;
                return;
            }
            if (_popup != null)
            {
                DGDebug.LogError<PopupContainer>($"Popup {newPopup.gameObject.name} is attaching but, {_popup.gameObject.name} is exists.");
            }
            _popup = newPopup;
            _popup.transform.SetParent(transform);
            // 스케일은 무시해야함. 그래야 스케일 애니메이션이 먹힘
            _popup.transform.ResetLocal(ignoreScale: true);
            gameObject.name = newPopup.GetType().ToString();

            switch (newPopup.BackgroundType)
            {
                case EPopupBackgroundType.Clear:
                {
                    _background.color = Color.clear;
                    break;
                }
                case EPopupBackgroundType.Black:
                {
                    _background.SetColorWithAlpha(Color.black, _defaultAlpha);
                    break;
                }
                case EPopupBackgroundType.White:
                {
                    _background.SetColorWithAlpha(Color.black, _defaultAlpha);
                    break;
                }
            }

            _background.raycastTarget = newPopup.BlockBackgroundInput;
        }

        public void SetSortOrder(int order)
        {
            _canvas.sortingOrder = order;
        }
    }
}