using Cysharp.Threading.Tasks;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.Audio;

namespace DragonGate
{
    public class AudioSourceWrapper : ICancelable
    {
        public AudioSource Source;
        public bool IsPaused;
        public bool IsPlaying => Source.isPlaying;
        public AudioClip clip { get { return Source.clip; } set { Source.clip = value; } }
        public GameObject gameObject => _gameObject;
        public Transform transform => _transform;
        public float minDistance { get { return Source.minDistance; } set { Source.minDistance = value; } }
        public float maxDistance { get { return Source.maxDistance; } set { Source.maxDistance = value; } }
        public bool loop { get { return Source.loop; } set { Source.loop = value; } }
        public float spatialBlend { get { return Source.spatialBlend; } set { Source.spatialBlend = value; } }
        public float volume { get { return Source.volume; } set { Source.volume = value; } }
        public bool playOnAwake { get { return Source.playOnAwake; } set { Source.playOnAwake = value; } }

        private float _playStartTime = -1;
        private Transform _transform;
        private GameObject _gameObject;
        private CancellationTokenSource _tokenSource;

        public AudioSourceWrapper(AudioSource source)
        {
            Source = source;
            _transform = source.transform;
            _gameObject = source.gameObject;
        }

        public void Play()
        {
            Source.Play();
            _playStartTime = Time.time;
        }

        public void PlayOneShot(AudioClip argClip, float fadeOut = -1)
        {
            Source.PlayOneShot(argClip);
            _playStartTime = Time.time;
            if (fadeOut > 0)
            {
                CancelToken();
                FadeOut(argClip.length, fadeOut).Forget();
            }
        }

        public void Stop()
        {
            Source.Stop();
        }

        public void Pause()
        {
            IsPaused = true;
            Source.Pause();
        }

        public void UnPause()
        {
            Source.UnPause();
            IsPaused = false;
        }

        public void SetParent(Transform parent)
        {
            Source.transform.SetParent(parent);
        }

        public void SetOutputMixerGroup(AudioMixerGroup group)
        {
            Source.outputAudioMixerGroup = group;
        }

        private async UniTask FadeOut(float clipLength, float fadeDuration)
        {
            float startVolume = Source.volume;
            float fadeStartTime = clipLength - fadeDuration;
            while (true)
            {
                if (Source == null) return;
                if (Source.volume <= 0f)
                {
                    break;
                }
                float elapsedTime = Time.time - _playStartTime;
                if (elapsedTime >= fadeStartTime)
                {
                    Source.volume = Mathf.Lerp(startVolume, 0, (elapsedTime - fadeStartTime) / fadeDuration);
                }
                await UniTaskHelper.Yield(this);
            }
        }

        public CancellationTokenSource GetTokenSource()
        {
            if (_tokenSource == null || _tokenSource.IsCancellationRequested)
            {
                _tokenSource = UniTaskHelper.CreateNormalGlobalLinkedToken();
            }
            return _tokenSource;
        }

        public void CancelToken()
        {
            UniTaskHelper.Cancel(_tokenSource);
            _tokenSource = null;
        }

        public bool IsValid()
        {
            return _tokenSource is { IsCancellationRequested: false };
        }
    }
}