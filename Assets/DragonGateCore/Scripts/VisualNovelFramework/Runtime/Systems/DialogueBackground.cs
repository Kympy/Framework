using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace DragonGate
{
    public class DialogueBackground : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _mainBackground;
        [SerializeField] private SpriteRenderer _subBackground;

        private SpriteRenderer _currentBackground;

        private void Awake()
        {
            _currentBackground = _mainBackground;
        }

        public async UniTask SwitchBackground(string key, float duration = -1)
        {
            var otherBackground = _currentBackground == _mainBackground ? _subBackground : _mainBackground;
            
            otherBackground.SetSprite(key);
            otherBackground.SetAlpha(0);
            var sequence = DOTween.Sequence();
            sequence.Append(otherBackground.DOFade(1, duration));
            sequence.Join(_currentBackground.DOFade(0, duration));
            await sequence.Play();
            _currentBackground.SetSprite(null);
            // 참조 전환
            _currentBackground = otherBackground;
            CameraManager.CurrentCamera.FitCameraToSpriteRenderer(_currentBackground);
        }

        public void SetBackground(string key)
        {
            _mainBackground.SetSprite(key);
            _mainBackground.SetAlpha(1);
            _subBackground.SetSprite(null);
            _subBackground.SetAlpha(0);
            _currentBackground = _mainBackground;
        }
    }
}
