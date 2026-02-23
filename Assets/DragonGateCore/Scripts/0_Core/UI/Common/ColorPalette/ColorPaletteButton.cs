using UnityEngine;
using UnityEngine.UI;

namespace DragonGate
{
    public class ColorPaletteButton : BetterButton
    {
        [SerializeField] private Image _image;
        
        private RectTransform _rectTransform;
        
        protected override void Awake()
        {
            base.Awake();
            if (_image == null)
            {
                TryGetComponent(out _image);
            }
            _rectTransform = transform as RectTransform;
        }

        public void SetColor(Color color)
        {
            _image.color = color;
        }

        public void Resize(Vector2 size)
        {
            _rectTransform.sizeDelta = size;
        }

        public void SetAnchoredPosition(Vector2 anchoredPosition)
        {
            _rectTransform.anchoredPosition = anchoredPosition;
        }
    }
}