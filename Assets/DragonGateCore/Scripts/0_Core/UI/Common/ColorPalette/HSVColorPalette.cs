using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = System.Object;

namespace DragonGate
{
    [RequireComponent(typeof(RawImage))]
    public class HSVColorPalette : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] private RawImage _rawImage;
        [SerializeField] private RectTransform _handle;

        public delegate void OnHSVChangedDelegate(HueSaturation hueSaturation);
        public OnHSVChangedDelegate OnHSVChanged;
        
        private void Awake()
        {
            if (_rawImage == null)
            {
                TryGetComponent(out _rawImage);
            }
            int radius = Mathf.FloorToInt(_rawImage.rectTransform.rect.width * 0.5f);
            _rawImage.texture = HSVColorExtensions.CreateColorWheel(radius);
            ToggleHandle(false);
        }

        private void OnDestroy()
        {
            Destroy(_rawImage);
            OnHSVChanged = null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            ToggleHandle(true);
            HandlePointerEvent(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            HandlePointerEvent(eventData);
            ToggleHandle(false);
        }

        public void OnDrag(PointerEventData eventData)
        {
            HandlePointerEvent(eventData);
        }
        
        private void HandlePointerEvent(PointerEventData eventData)
        {
            // 로컬 좌표로 변환
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rawImage.rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var localPos
            );
            
            // Debug.Log($"Pointer LocalPos: {localPos}, ScreenPos: {eventData.position}");
        
            // localPos를 이용해 클릭한 위치의 픽셀/색상 계산 가능
            var result = HSVColorExtensions.GetHueSaturation(localPos, _rawImage.rectTransform);
            if (result != null)
            {
                OnHSVChanged(result.Value);
                if (_handle != null)
                {
                    _handle.anchoredPosition = localPos;
                }
            }
            else
            {
                _handle.anchoredPosition = Vector2.ClampMagnitude(localPos, _rawImage.rectTransform.rect.width * 0.5f);
            }
        }

        public void ToggleHandle(bool isOn)
        {
            if (_handle == null) return;
            _handle.SetActive(isOn);
        }
    }
}