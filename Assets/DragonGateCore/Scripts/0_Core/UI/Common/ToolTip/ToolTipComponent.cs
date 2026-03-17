using UnityEngine;
using UnityEngine.Localization;

namespace DragonGate
{
    [RequireComponent(typeof(BetterButton))]
    public class ToolTipComponent : MonoBehaviour
    {
        [SerializeField] private LocalizedString _tooltipText;

        private BetterButton _betterButton;
        private UIToolTip _toolTip;

        private void Awake()
        {
            TryGetComponent(out _betterButton);
            if (_betterButton == null) return;
            _betterButton.OnEnter?.AddListener(() =>
            {
                if (_toolTip == null)
                    _toolTip = UIManager.Instance.ShowToolTip(transform as RectTransform, _tooltipText);
                _toolTip.SetVisible();
            });
            _betterButton.OnExit?.AddListener(() =>
            {
                if (_toolTip != null)
                {
                    _toolTip.SetHidden();
                    _toolTip = null;
                }
            });
        }

        private void OnDisable()
        {
            if (_toolTip == null) return;
            _toolTip.SetHidden();
            _toolTip = null;
        }

        private void OnDestroy()
        {
            if (_toolTip == null) return;
            _toolTip.SetHidden();
            _toolTip = null;
        }
    }
}