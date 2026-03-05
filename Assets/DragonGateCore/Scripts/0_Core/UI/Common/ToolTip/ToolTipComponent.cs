using System;
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
            _toolTip = ToolTipManager.Instance.GetToolTip(transform as RectTransform, _tooltipText);
            _betterButton.OnEnter?.AddListener(() =>
            {
                _toolTip.SetVisible();
            });
            _betterButton.OnExit?.AddListener(() =>
            {
                _toolTip.SetHidden();
            });
        }

        private void OnDisable()
        {
            if (_toolTip == null) return;
            if (_toolTip.IsVisible == false) return;
            _toolTip.SetHidden();
        }

        private void OnDestroy()
        {
            if (_toolTip == null) return;
            ToolTipManager.Instance?.HideToolTip(_toolTip);
        }
    }
}
