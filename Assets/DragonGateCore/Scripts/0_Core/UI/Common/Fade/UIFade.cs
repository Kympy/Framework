using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DragonGate
{
    public struct FadeData
    {
        public float InDuration;
        public float OutDuration;
        public Color InStartColor;
        public Color InEndColor;
        public Color OutStartColor;
        public Color OutEndColor;
    }
    
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GraphicRaycaster))]
    [RequireComponent(typeof(CanvasScaler))]
    public class UIFade : CoreBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private Gradient _curve;

        private Color _endColor;
        private float _duration;
        private float _elapsed;

        private void Awake()
        {
            TryGetComponent(out _image);
        }

        public void SetStartColor(Color color)
        {
            _image.color = color;

            if (_curve == null) return;

            var colorKeys = _curve.colorKeys;
            if (colorKeys != null && colorKeys.Length > 0)
            {
                colorKeys[0].color = color;
                _curve.colorKeys = colorKeys;
            }
        }

        public void SetEndColor(Color color)
        {
            if (_curve == null) return;

            var colorKeys = _curve.colorKeys;
            if (colorKeys != null && colorKeys.Length > 0)
            {
                colorKeys[^1].color = color;
                _curve.colorKeys = colorKeys;
            }
        }

        public async UniTask Play(float duration)
        {
            gameObject.SetActive(true);
            _elapsed = 0f;
            while (_elapsed < duration)
            {
                await UniTaskHelper.Yield(this);
                _elapsed += Time.deltaTime;
                var curveColor = _curve.Evaluate(_elapsed / duration);
                _image.color = curveColor;
            }
            gameObject.SetActive(false);
        }

        public async UniTask FromBlackToTransparent(float duration, ICancelable cancelable = null)
        {
            SetStartColor(Color.black);
            SetEndColor(Color.clear);
            await Play(duration);
        }

        public async UniTask FromTransparentToBlack(float duration)
        {
            SetStartColor(Color.clear);
            SetEndColor(Color.black);
            await Play(duration);
        }
    }
}
