using DG.Tweening;
using UnityEngine;

namespace DragonGate
{
    public enum EUIEffectType
    {
        DOTween,
        Custom,
    }

    public abstract class UIEffect : CoreBehaviour, IPoolable
    {
        [SerializeField] protected EUIEffectType _effectType;
        [SerializeField] protected DOTweenAnimation[] _doTweenAnimations;
        [SerializeField] protected Animator _animator;
        [SerializeField] private int _overrideSortOrder = -1;

        private DOTweenAnimation _longestDOTweenAnimation;

        public int SortOrder => _overrideSortOrder >= 0 ? _overrideSortOrder : UISortOrder.Effect;

        public override void Init()
        {
            base.Init();

            if (_effectType == EUIEffectType.DOTween)
            {
                if (_longestDOTweenAnimation == null)
                {
                    _longestDOTweenAnimation = FindLongestDOTweenAnimation();
                }

                _longestDOTweenAnimation.tween.onComplete = ReturnToPool;
            }
        }

        public void Play()
        {
            PlayInternal();

            if (_effectType == EUIEffectType.DOTween)
            {
                foreach (var tween in _doTweenAnimations)
                {
                    tween.DORestart();
                }
            }
        }

        protected virtual void PlayInternal()
        {
        }

        protected void ReturnToPool()
        {
            PoolManager.Instance?.ReturnComponent(this);
        }

        private DOTweenAnimation FindLongestDOTweenAnimation()
        {
            float longestLength = 0;
            DOTweenAnimation longestTween = null;
            foreach (var tween in _doTweenAnimations)
            {
                float length = tween.delay + tween.duration;
                if (length > longestLength)
                {
                    longestLength = length;
                    longestTween = tween;
                }
            }
            return longestTween;
        }

        public virtual void OnGet()
        {
        }

        public virtual void OnReturn()
        {
        }
    }
}