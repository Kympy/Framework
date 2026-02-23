using System;
using DragonGate;
using UnityEngine;

namespace Framework
{
    public enum PaletteOrientation { Horizontal, Vertical }

    public class StaticColorPaletteBlock : UICore
    {
        [SerializeField] private ColorPaletteButton _paletteButtonPrefab;
        [Space] 
        [SerializeField] private Color[] _baseColors = new Color[]
        {
            Color.white,
            Color.red,
            ColorFactory.Orange,
            Color.yellow,
            ColorFactory.Lime,
            Color.green,
            Color.cyan,
            Color.blue,
            ColorFactory.Violet,
        };
        [SerializeField] private int _stepCount = 6;
        [SerializeField] private PaletteOrientation _orientation = PaletteOrientation.Horizontal;
        
        private float _colorStepValue;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            CreatePaletteButtons();
        }

        private void CreatePaletteButtons()
        {
            for (int c = _rectTransform.childCount - 1; c >= 0; c--)
            {
                var child = _rectTransform.GetChild(c);
#if UNITY_EDITOR
                if (!Application.isPlaying) UnityEngine.Object.DestroyImmediate(child.gameObject);
                else UnityEngine.Object.Destroy(child.gameObject);
#else
                UnityEngine.Object.Destroy(child.gameObject);
#endif
            }

            float buttonWidth = _rectTransform.rect.width / _stepCount;
            float buttonHeight = _rectTransform.rect.height / _baseColors.Length;
            Vector2 buttonSize = new Vector2(buttonWidth, buttonHeight);
            float buttonHalfWidth = buttonWidth * 0.5f;
            float buttonHalfHeight = buttonHeight * 0.5f;
            
            _colorStepValue = 1f / _stepCount;

            for (int i = 0; i < _baseColors.Length; i++)
            {
                Color.RGBToHSV(_baseColors[i], out float h, out float s, out float v);
                for (int j = 0; j < _stepCount; j++)
                {
                    Color newColor = Color.HSVToRGB(h, s, 1f - _colorStepValue * j);
                    var button = Instantiate(_paletteButtonPrefab, _rectTransform);
                    button.SetColor(newColor);

                    // Force anchors to proportional cell so it fills perfectly regardless of prefab anchors
                    var rt = (RectTransform)button.transform;
                    float xMin, xMax, yMin, yMax;
                    if (_orientation == PaletteOrientation.Horizontal)
                    {
                        // Base colors stacked vertically, steps progress left->right
                        xMin = (float)j / _stepCount;
                        xMax = (float)(j + 1) / _stepCount;
                        yMin = (float)i / _baseColors.Length;
                        yMax = (float)(i + 1) / _baseColors.Length;
                    }
                    else // PaletteOrientation.Vertical
                    {
                        // Base colors laid out horizontally, steps progress bottom->top
                        xMin = (float)i / _baseColors.Length;
                        xMax = (float)(i + 1) / _baseColors.Length;
                        yMin = (float)j / _stepCount;
                        yMax = (float)(j + 1) / _stepCount;
                    }

                    rt.anchorMin = new Vector2(xMin, yMin);
                    rt.anchorMax = new Vector2(xMax, yMax);
                    rt.pivot = new Vector2(0.5f, 0.5f);

                    // Zero the offsets so it stretches to the cell exactly
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;

                    // Ensure no inherited scale issues
                    rt.localScale = Vector3.one;
                }
            }
        }
    }
}