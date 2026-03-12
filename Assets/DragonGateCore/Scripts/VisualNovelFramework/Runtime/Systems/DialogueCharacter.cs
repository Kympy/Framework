using System;
using DG.Tweening;
using UnityEngine;

namespace DragonGate
{
    public class DialogueCharacter : CoreBehaviour
    {
        [SerializeField] protected SpriteRenderer _spriteRenderer;
        [SerializeField] protected Animator _animator;

        protected Tween _moveTween;
        protected Tween _fadeTween;
        protected Tween _alphaTween;

        protected virtual void Awake()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
        }

        public void SetAnimationTrigger(string trigger)
        {
            _animator.SetTrigger(trigger);
        }

        public void TeleportTo(Vector3 position)
        {
            _moveTween?.Kill();
            _moveTween = null;
            transform.position = position;
        }

        public Tween MoveTo(Vector3 worldPosition, Ease easeType, float duration, float scale = 1f)
        {
            _moveTween?.Kill();
            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOMove(worldPosition, duration).SetEase(easeType));
            sequence.Append(transform.DOScale(scale, duration).SetEase(easeType));
            _moveTween = sequence;
            return _moveTween;
        }

        public Tween FadeColor(Color start, Color end, float duration)
        {
            _fadeTween?.Kill();
            _spriteRenderer.color = start;
            return _spriteRenderer.DOColor(end, duration);
        }

        public Tween ToTransparent(float duration)
        {
            _alphaTween?.Kill();
            return _spriteRenderer.DOFade(0, duration);
        }

        public Tween ToVisible(float duration)
        {
            _alphaTween?.Kill();
            return _spriteRenderer.DOFade(1, duration);
        }

        public void SetColor(Color endColor)
        {
            _fadeTween?.Kill();
            _spriteRenderer.color = endColor;
        }
    }
}
