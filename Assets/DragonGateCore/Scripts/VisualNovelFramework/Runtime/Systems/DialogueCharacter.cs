using System;
using DG.Tweening;
using UnityEngine;

namespace DragonGate
{
    public class DialogueCharacter : CoreBehaviour
    {
        [SerializeField] protected SpriteRenderer _spriteRenderer;
        [SerializeField] protected Animator _animator;

        protected Tweener _moveTween;
        protected Tweener _fadeTween;

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

        public Tween MoveTo(Vector3 worldPosition, Ease easeType, float duration)
        {
            _moveTween?.Kill();
            _moveTween = transform.DOMove(worldPosition, duration).SetEase(easeType);
            return _moveTween;
        }

        public Tween FadeColor(Color start, Color end, float duration)
        {
            _fadeTween?.Kill();
            _spriteRenderer.color = start;
            return _spriteRenderer.DOColor(end, duration);
        }
    }
}
