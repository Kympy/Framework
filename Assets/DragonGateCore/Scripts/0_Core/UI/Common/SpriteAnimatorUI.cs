using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DragonGate
{
    public class SpriteAnimatorUI : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [Header("Animation")]
        [SerializeField] private List<Sprite> _sprites = new();
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

        private void Awake()
        {
            if (_image == null)
            {
                TryGetComponent(out _image);
            }

            if (_sprites != null && _sprites.Count > 0)
                _image.sprite = _sprites[0];

            if (_autoPlay)
                _isPlaying = true;

            if (_sprites != null && _sprites.Count > 0)
                _frameInterval = _duration / _sprites.Count;
        }

        private void Update()
        {
            if (_isPlaying == false || _sprites == null || _sprites.Count == 0) return;
            if (_loop == false && _currentFrame >= _sprites.Count - 1)
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
                return;
            }
            _timer += Time.deltaTime;
            if (_timer >= _frameInterval)
            {
                _currentFrame = (_currentFrame + 1) % _sprites.Count;
                SetSprite(_currentFrame);
                _timer -= _frameInterval;
            }
        }

        private void OnEnable()
        {
            if (_autoPlay)
                _isPlaying = true;
        }

        private void OnDisable()
        {
            if (_autoPlay == false)
                _isPlaying = false;
        }

        private void SetSprite(int frame)
        {
            if (_sprites != null && frame >= 0 && frame < _sprites.Count)
                _image.sprite = _sprites[frame];
        }

        public void Clear()
        {
            if (_sprites != null)
            {
                _sprites.Clear();
            }
        }

        public void SetSprites(Sprite[] sprites)
        {
            _sprites.Clear();
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                _sprites.Add(sprites[i]);
            }
            if (_sprites != null && _sprites.Count > 0)
                _frameInterval = _duration / _sprites.Count;
        }

        public void PlayAtStart()
        {
            _currentFrame = 0;
            _timer = 0f;
            SetSprite(0);
            _isPlaying = true;
        }

        public void SetAutoDestroy(bool autoDestroy)
        {
            _autoDestroy = autoDestroy;
        }

        public void SetDuration(float duration)
        {
            _duration = duration;
            if (_sprites != null && _sprites.Count > 0)
                _frameInterval = duration / _sprites.Count;
        }

#if UNITY_EDITOR
        public void EditorTick(float deltaTime)
        {
            if (_sprites == null || _sprites.Count == 0 || _image == null)
                return;

            _timer += deltaTime;
            float frameInterval = _duration / _sprites.Count;
            if (_timer >= frameInterval)
            {
                SetSprite(_currentFrame % _sprites.Count);
                _currentFrame++;
                _timer -= frameInterval;
            }
        }
#endif
    }
}
