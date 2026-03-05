using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace DragonGate
{
    public partial class UICore
    {
        // 공용 애니메이션 타입
        protected enum EAnimationType { None, FadeIn, FadeOut, ScaleUp, ScaleDown }
        [Header("Animation Type")]
        [SerializeField] protected EAnimationType _openAnimation = EAnimationType.None;
        [SerializeField] protected EAnimationType _closeAnimation = EAnimationType.None;

        
        protected CanvasGroup CanvasGroup => _canvasGroup == null ? gameObject.GetOrAddComponent<CanvasGroup>() : _canvasGroup;
        
        private CanvasGroup _canvasGroup;
        private Coroutine _animationCoroutine;
        private const float _fadeSpeed = 5f;

        protected void Animate(EAnimationType animationType, UnityAction onComplete = null)
        {
            if (isActiveAndEnabled == false) return;
            StartCoroutine(PlayAnimation(animationType, onComplete));
        }

        private IEnumerator PlayAnimation(EAnimationType animationType, UnityAction onComplete)
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }

            var targetCoroutine = GetAnimationCoroutine(animationType);
            if (targetCoroutine != null)
            {
                _animationCoroutine = StartCoroutine(targetCoroutine);
                yield return _animationCoroutine;
            }

            onComplete?.Invoke();
        }

        private IEnumerator GetAnimationCoroutine(EAnimationType animationType)
        {
            switch (animationType)
            {
                case EAnimationType.FadeIn:
                {
                    CanvasGroup.alpha = 0;
                    return Fade(1);
                }
                case EAnimationType.FadeOut: return Fade(0);
                case EAnimationType.ScaleUp: return ScaleUp();
                case EAnimationType.ScaleDown: return ScaleDown();
                default: return null;
            }
        }

        protected void HideWithAnimation(UnityAction callback = null)
        {
            if (callback == null)
            {
                callback = SetActiveFalse;
            }
            else
            {
                callback += SetActiveFalse;
            }
            Animate(_closeAnimation, callback);
        }

        private IEnumerator Fade(float targetAlpha)
        {
            var canvasGroup = CanvasGroup;
            bool decrease = targetAlpha <= canvasGroup.alpha; 
            while (canvasGroup.alpha.IsSame(targetAlpha) == false)
            {
                yield return null;
                if (decrease)
                {
                    canvasGroup.alpha -= Time.unscaledDeltaTime * _fadeSpeed;
                }
                else
                {
                    canvasGroup.alpha += Time.unscaledDeltaTime * _fadeSpeed;
                }
                canvasGroup.alpha = canvasGroup.alpha.Clamp01();
            }
        }

        private IEnumerator ScaleUp()
        {
            return Scaling(Vector3.one * 0.01f, Vector3.one);
        }

        private IEnumerator ScaleDown()
        {
            return Scaling(transform.localScale, Vector3.zero);
        }

        private IEnumerator Scaling(Vector3 startScale, Vector3 endScale)
        {
            transform.localScale = startScale;
            while (true)
            {
                yield return null;
                var current = transform.localScale;
                if ((current - endScale).sqrMagnitude <= 0.01f)
                {
                    break;
                }
                transform.localScale = Vector3.Lerp(transform.localScale, endScale, Time.unscaledDeltaTime * 30f);
            }
            transform.localScale = endScale;
        }

        private void SetActiveFalse()
        {
            gameObject.SetActive(false);
        } 
    }
}