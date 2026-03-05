using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [Header("Animation")]
        [SerializeField] private Sprite[] _sprites;
        [Header("Settings")]
        [SerializeField] private bool _autoPlay = true;
        [SerializeField] private bool _autoDisable = true;
        [SerializeField] private bool _autoDestroy = false;
        [SerializeField] private bool _loop = false;
        [SerializeField] private float _duration = 1f;

        private int _currentFrame = 0;
        private float _timer = 0f;
        private float _frameInterval = 0.1f;
        private bool _isPlaying = false;
        private Dictionary<int, List<Action>> _events;

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                TryGetComponent(out _spriteRenderer);
            }
            _spriteRenderer.sprite = _sprites[0];
            if (_autoPlay)
                _isPlaying = true;
            
            _frameInterval = _duration / _sprites.Length;
        }

        private void Update()
        {
            if (_isPlaying == false) return;
            if (_loop == false && _currentFrame >= _sprites.Length - 1)
            {
                if (_autoDestroy)
                {
                    Destroy(gameObject);
                    return;
                }
                if (_autoDisable)
                {
                    gameObject.SetActive(false);
                    return;
                }
                _spriteRenderer.enabled = false;
                return;
            }
            _timer += Time.deltaTime;
            
            if (_timer < _frameInterval) return;
            
            _currentFrame = (_currentFrame + 1) % _sprites.Length;
            SetSprite(_currentFrame);
            InvokeEvent(_currentFrame);
            _timer -= _frameInterval;
        }

        private void OnEnable()
        {
            _currentFrame = 0;
            _timer = 0f;
            SetSprite(_currentFrame);
            if (_autoPlay)
                _isPlaying = true;
                
            _spriteRenderer.enabled = true;
        }

        private void OnDisable()
        {
            if (_autoPlay == false)
                _isPlaying = false;
        }

        private void SetSprite(int frame)
        {
            _spriteRenderer.sprite = _sprites[frame];
        }

        public void PlayAtStart()
        {
            _currentFrame = 0;
            _timer = 0f;
            SetSprite(_currentFrame);
            _isPlaying = true;
            _spriteRenderer.enabled = true;
        }

        public void SetAutoDestroy(bool autoDestroy)
        {
            _autoDestroy = autoDestroy;
        }

        public void SetDuration(float duration)
        {
            _duration = duration;
            _frameInterval = duration / _sprites.Length;
        }

        public void AddEvent(int frame, Action action)
        {
            _events ??= new();
            if (_events.TryGetValue(frame, out var list) == false)
            {
                _events.Add(frame, new List<Action>());
                list = _events[frame];
            }
            list.Add(action);
        }

        public void AddEventLast(Action action)
        {
            _events ??= new();
            int frame = _sprites.Length - 1;
            if (_events.TryGetValue(frame, out var list) == false)
            {
                _events.Add(frame, new List<Action>());
                list = _events[frame];
            }
            list.Add(action);
        }

        public void ClearAllEvents()
        {
            _events?.Clear();
        }

        public void RemoveEvent(int frame)
        {
            _events?.Remove(frame);
        }

        public void RemoveEvent(int frame, Action action)
        {
            if (_events == null) return;
            if (_events.TryGetValue(frame, out var list) == false) return;
            list.Remove(action);
        }

        private void InvokeEvent(int frame)
        {
            if (_events == null) return;
            if (_events.TryGetValue(frame, out var list) == false)
            {
                return;
            }
            for (int i = 0; i < list.Count; i++)
            {
                list[i]?.Invoke();
            }
        }

#if UNITY_EDITOR
        public void EditorTick(float deltaTime)
        {
            if (_sprites == null || _sprites.Length == 0 || _spriteRenderer == null)
                return;

            _timer += deltaTime;
            float frameInterval = _duration / _sprites.Length;
            if (_timer >= frameInterval)
            {
                SetSprite(_currentFrame % _sprites.Length);
                _currentFrame++;
                _timer -= frameInterval;
            }
        }
#endif
    }
}