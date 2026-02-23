using UnityEngine;
using UnityEngine.UI;

namespace DragonGate
{
    public class HSVColorPaletteBlock : UICore
    {
        [SerializeField] private HSVColorPalette _palette;
        [SerializeField] private Slider _valueSlider;
        [SerializeField] private Image _colorPreview;
        [Space] [SerializeField] private float _gamma = 0.66f;

        private HSVColor _currentHSVColor;
        private Color _currentRGBColor;

        public delegate void OnColorChangedDelegate(Color color);

        public OnColorChangedDelegate OnColorChanged;

        private void Awake()
        {
            _palette.OnHSVChanged -= OnHsvChanged;
            _palette.OnHSVChanged += OnHsvChanged;

            if (_colorPreview != null)
            {
                OnColorChanged -= PreviewColor;
                OnColorChanged += PreviewColor;
            }
            _valueSlider.maxValue = 1;
            _valueSlider.minValue = 0;
            _valueSlider.onValueChanged.AddListener(OnValueChanged);
            _valueSlider.value = 1f;
        }

        protected virtual void OnValueChanged(float value)
        {
            // 감마 보정 (2.2는 보통 모니터 감마 값)
            float adjustedValue = Mathf.Pow(value, _gamma);
            var newColor = _currentHSVColor;
            newColor.V = adjustedValue;
            _currentHSVColor = newColor;
            _currentRGBColor = newColor.HSVToRGB();
            OnColorChanged?.Invoke(_currentRGBColor);
        }

        private void OnHsvChanged(HueSaturation hueSaturation)
        {
            var current = _currentHSVColor;
            current.H = hueSaturation.Hue;
            current.S = hueSaturation.Saturation;
            _currentHSVColor = current;
            _currentRGBColor = current.HSVToRGB();
            OnColorChanged?.Invoke(_currentRGBColor);
        }

        public void PreviewColor(Color color)
        {
            if (_colorPreview != null)
                _colorPreview.color = color;
        }
    }
}