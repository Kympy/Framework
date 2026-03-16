using UnityEngine;
using UnityEngine.Events;

namespace DragonGate
{
    public abstract class UIToolTipBase : UICore, IPoolable
    {
        [SerializeField] private RenderMode _originalRenderMode = RenderMode.ScreenSpaceOverlay;
        [SerializeField] private float _delay = 1f;

        private TimerHandle _timer;
        private Canvas _canvas;

        protected virtual void Awake()
        {
            if (TryGetComponent(out _canvas))
            {
                _canvas.renderMode = _originalRenderMode;
                _canvas.overrideSorting = true;
                _canvas.sortingOrder = UISortOrder.ToolTip;
            }
        }

        public override void SetVisible(UnityAction onVisible = null)
        {
            _timer.Clear();
            if (_delay > 0f)
                _timer = TimerManager.SetDelayTimer(_delay, () => base.SetVisible(onVisible), ignoreTimeScale: true);
            else
                base.SetVisible(onVisible);
        }

        public override void SetHidden(UnityAction onHidden = null)
        {
            _timer.Clear();
            base.SetHidden(onHidden);
        }

        public virtual void OnGet()
        {
            // 풀 반환 시 non-Canvas 오브젝트 하위로 들어가면 Unity가 Canvas 모드를 World Space로 바꿈.
            // 꺼낼 때 원본 모드로 복구한다.
            if (_canvas != null)
                _canvas.renderMode = _originalRenderMode;
        }

        public virtual void OnReturn()
        {
            _timer.Clear();
        }
    }
}