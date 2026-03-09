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
        
        private float _duration;
        private float _elapsed;

        private void Awake()
        {
            TryGetComponent(out _image);
            _curve.colorKeys = new GradientColorKey[2];
        }

        public void SetInOutColor(Color inColor, Color outColor)
        {
            _image.color = inColor;
            var colorKeys = _curve.colorKeys;
            colorKeys[0] = new GradientColorKey(inColor, 0f);
            colorKeys[1] = new GradientColorKey(outColor, 1f);

            var alphaKeys = _curve.alphaKeys;
            alphaKeys[0] = new GradientAlphaKey(inColor.a, 0f);
            alphaKeys[1] = new GradientAlphaKey(outColor.a, 1f);
            
            _curve.SetKeys(colorKeys, alphaKeys);
        }

        public async UniTask Play(float duration, ICancelable cancelable = null)
        {
            gameObject.SetActive(true);
            _elapsed = 0f;
            while (_elapsed < duration)
            {
                await UniTaskHelper.Yield(cancelable ?? this);
                if (cancelable?.IsValidCancelToken() == false)
                {
                    DGDebug.Log("UI Fade canceled.");
                    break;
                }
                _elapsed += Time.deltaTime;
                var curveColor = _curve.Evaluate(_elapsed / duration);
                _image.color = curveColor;
            }
            gameObject.SetActive(false);
        }

        public async UniTask FromBlackToTransparent(float duration, ICancelable cancelable = null)
        {
            SetInOutColor(Color.black, Color.clear);
            await Play(duration, cancelable);
        }

        public async UniTask FromTransparentToBlack(float duration, ICancelable cancelable = null)
        {
            SetInOutColor(Color.clear, Color.black);
            await Play(duration, cancelable);
        }
    }
}
