using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace DragonGate
{
    public partial class UIManager
    {
        public UIToolTip ShowToolTip(RectTransform anchor, string message)
        {
            var toolTip = ToolTipManager.Instance.GetToolTip(anchor, message);
            toolTip.SetVisible();
            return toolTip;
        }

        public UIToolTip ShowToolTip(RectTransform anchor, TableEntryReference entryReference)
        {
            var toolTip = ToolTipManager.Instance.GetToolTip(anchor, entryReference);
            toolTip.SetVisible();
            return toolTip;
        }

        public UIToolTip ShowToolTip(RectTransform anchor, LocalizedString localizedString)
        {
            var toolTip = ToolTipManager.Instance.GetToolTip(anchor, localizedString);
            toolTip.SetVisible();
            return toolTip;
        }

        public T ShowToolTip<T>(RectTransform anchor, string resourceKey) where T : UIToolTipBase
        {
            var toolTip = ToolTipManager.Instance.GetToolTip<T>(anchor, resourceKey);
            toolTip.SetVisible();
            return toolTip;
        }

        public T ShowToolTip3D<T>(Transform target, string resourceKey) where T : UIToolTip3D
        {
            var toolTip = ToolTipManager.Instance.GetToolTip3D<T>(target, resourceKey);
            toolTip.SetVisible();
            return toolTip;
        }

        public void HideToolTip(UIToolTipBase toolTip)
            => ToolTipManager.Instance.HideToolTip(toolTip);
    }
}