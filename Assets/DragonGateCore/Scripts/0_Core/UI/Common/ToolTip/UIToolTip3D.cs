using UnityEngine;

namespace DragonGate
{
    public class UIToolTip3D : UIToolTipBase, GameLoop.IGameUpdate
    {
        [SerializeField] protected float _offsetY;

        protected Transform _target;
        private RectTransform _parentCanvasRect;

        public void SetTarget(Transform target, RectTransform parentCanvasRect)
        {
            _target = target;
            _parentCanvasRect = parentCanvasRect;
        }

        protected override void OnVisible()
        {
            base.OnVisible();
            GameLoop.RegisterUpdate(this);
        }

        protected override void OnHidden()
        {
            base.OnHidden();
            GameLoop.UnregisterUpdate(this);
        }

        public bool IgnoreTimeScale { get; } = true;

        public void OnUpdate(float deltaTime)
        {
            if (_target == null || CameraManager.CurrentCamera == null || _parentCanvasRect == null)
            {
                HideSelf();
                return;
            }

            Vector2 screenPoint = CameraManager.WorldToScreenPoint(_target.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentCanvasRect,
                screenPoint,
                null,
                out Vector2 localPoint);

            RectTransform.anchoredPosition = localPoint + new Vector2(0, _offsetY);
        }
    }
}