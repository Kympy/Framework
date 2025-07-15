using UnityEngine;

namespace Framework
{
    public abstract class PopupBase : MonoBehaviour
    {
        public abstract void BeforeShow(IPopupParameter parameter);
    }
}