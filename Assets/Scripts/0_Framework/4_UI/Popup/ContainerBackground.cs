using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Framework
{
    public class ContainerBackground : MonoBehaviour, IPointerUpHandler
    {
        public bool AllowBackgroundHide { get; set; } = true;
        
        [SerializeField] private Image _image;

        private void Awake()
        {
            _image.raycastTarget = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerEnter != _image.gameObject)
            {
                return;
            }
            if (AllowBackgroundHide == false) return;
            PopupManager.Instance.ClosePopup();
        }
    }
}