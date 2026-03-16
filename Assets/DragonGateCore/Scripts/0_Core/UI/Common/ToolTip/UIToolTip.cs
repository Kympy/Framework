using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace DragonGate
{
    public class UIToolTip : UIToolTipBase
    {
        [UnityEngine.SerializeField] private LocalizedTextMeshProUGUI _message;

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
    }
}