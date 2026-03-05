using UnityEngine;
using UnityEngine.Events;

namespace DragonGate
{
    public partial class UICore : CoreBehaviour
    {
        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                    _rectTransform = transform as RectTransform;
                return _rectTransform;
            }
        }
        
        private RectTransform _rectTransform;

        public bool IsVisible => gameObject.activeSelf;

        public virtual void SetVisible(UnityAction onVisible = null)
        {
            gameObject.SetActive(true);
            OnVisible();
            if (_openAnimation != EAnimationType.None)
            {
                Animate(_openAnimation, onVisible);
            }
            else
            {
                onVisible?.Invoke();
            }
        }

        public virtual void SetHidden(UnityAction onHidden = null)
        {
            if (gameObject.activeSelf == false)
            {
                return;
            }
            if (_closeAnimation != EAnimationType.None)
            {
                if (onHidden != null)
                    onHidden += OnHidden;
                else
                    onHidden = new UnityAction(OnHidden);
                HideWithAnimation(onHidden);
            }
            else
            {
                gameObject.SetActive(false);
                OnHidden();
                onHidden?.Invoke();
            }
        }

        protected virtual void OnVisible() { }

        protected virtual void OnHidden() { }

        protected void HideSelf() => UIManager.Instance?.Hide(this);
    }
}