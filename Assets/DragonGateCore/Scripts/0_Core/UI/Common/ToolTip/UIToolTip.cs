using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace DragonGate
{
    public class UIToolTip : UICore, IPoolable
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private LocalizedTextMeshProUGUI _message;
        [SerializeField] private float _delay = 1f;
        
        private TimerHandle _timer;

        private void Awake()
        {
            TryGetComponent(out _canvas);
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = UISortOrder.ToolTip;
        }

        public UIToolTip SetMessage(string message)
        {
            _message.SetText(message);
            return this;
        }

        public UIToolTip SetMessage(TableEntryReference tableEntryReference)
        {
            _message.SetTextKey(tableEntryReference);
            return this;
        }

        public UIToolTip SetMessage(LocalizedString localizedString)
        {
            _message.LocalizedStringRef.TableReference = localizedString.TableReference;
            _message.LocalizedStringRef.TableEntryReference = localizedString.TableEntryReference;
            _message.Refresh();
            return this;
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

        public void OnGet()
        {
            
        }

        public void OnReturn()
        {
            _timer.Clear();
        }
    }
}
