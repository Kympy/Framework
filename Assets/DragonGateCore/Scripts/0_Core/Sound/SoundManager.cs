using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Audio;

namespace DragonGate
{
    public class SoundManager : Singleton<SoundManager>, GameLoop.IGameUpdate, ICancelable
    {
        public bool IgnoreTimeScale { get; } = false;
    
        private string _lastBgmKey = null;
        public string CurrentBgmKey => _lastBgmKey;
        private AudioMixer _masterMixer;
        private AudioMixerGroup _bgmGroup;
        private AudioMixerGroup _sfxGroup;
        private AudioSourceWrapper _bgmAudioSource;
        private CancellationTokenSource _tokenSource;
        private Transform _audioSourceRoot;
        private Stack<AudioSourceWrapper> _audioSourcePool = new Stack<AudioSourceWrapper>();
        private List<AudioSourceWrapper> _activeAudioSource = new List<AudioSourceWrapper>(10);

        protected override void OnCreate()
        {
            base.OnCreate();
            _masterMixer = Resources.Load<AudioMixer>("Sound/MasterMixer");
            _bgmGroup = _masterMixer.FindMatchingGroups("BGM")[0];
            _sfxGroup = _masterMixer.FindMatchingGroups("SFX")[0];
            
            _audioSourceRoot = new GameObject("Audio Source").transform;
            Object.DontDestroyOnLoad(_audioSourceRoot);
            CreateBgmAudioSource();
            
            Register();
        }
        
        public void OnUpdate(float deltaTime)
        {
            for (int i = _activeAudioSource.Count - 1; i >= 0; i--)
            {
                var source = _activeAudioSource[i];
                if (source.IsPaused) continue;
                if (source.IsPlaying == false)
                {
                    ReturnAudioSource(source);
                    _activeAudioSource.RemoveAtSwapBack(i);
                }
            }
        }

        protected void OnDestroy()
        {
            Unregister();
        }

        public void PauseSFX()
        {
            foreach (var source in _activeAudioSource)
            {
                source.Pause();
            }
        }

        public void UnPauseSFX()
        {
            foreach (var source in _activeAudioSource)
            {
                source.UnPause();
            }
        }

        public void CreateBgmAudioSource()
        {
            if (_bgmAudioSource == null)
            {
                var sourceObject = new GameObject("BgmAudioSource").AddComponent<AudioSource>();
                _bgmAudioSource = new AudioSourceWrapper(sourceObject);
                _bgmAudioSource.playOnAwake = false;
                _bgmAudioSource.loop = true;
                _bgmAudioSource.spatialBlend = 0;
                _bgmAudioSource.SetOutputMixerGroup(_bgmGroup);
                Object.DontDestroyOnLoad(_bgmAudioSource.gameObject);
            }
        }
        
        public void PlayOneShot(string key, float volume = 1, float fadeOut = -1f)
        {
            var clip = AssetManager.Instance.GetAsset<AudioClip>(key);
            var source = GetAudioSource();
            source.spatialBlend = 0;
            source.loop = false;
            source.volume = volume;
            source.PlayOneShot(clip, fadeOut);
            _activeAudioSource.Add(source);
        }

        public void PlayOneShotAt(string key, Vector3 position, float volume = 1f, float fadeOut = -1f)
        {
            var clip = AssetManager.Instance.GetAsset<AudioClip>(key);
            var source = GetAudioSource();
            source.spatialBlend = 1;
            source.loop = false;
            source.volume = volume;
            source.transform.position = position;
            source.PlayOneShot(clip, fadeOut);
            _activeAudioSource.Add(source);
        }

        public async UniTask WaitPlayOneShot(string key, float volume = 1f, float fadeOut = -1f, ICancelable cancelable = null)
        {
            var clip = AssetManager.Instance.GetAsset<AudioClip>(key);
            var source = GetAudioSource();
            source.spatialBlend = 0;
            source.loop = false;
            source.volume = volume;
            _activeAudioSource.Add(source);
            await source.WaitPlayOneShot(clip, cancelable, fadeOut);
        }

        public async UniTaskVoid PlayBGM(string key, float volume = 1f, bool crossFade = false, float fadeDuration = 0.2f)
        {
            CancelToken();
            if (_lastBgmKey == key) return;
            if (_bgmAudioSource.clip != null || _bgmAudioSource.IsPlaying)
            {
                if (crossFade == false)
                {
                    _bgmAudioSource.Stop();
                    _bgmAudioSource.clip = null;
                }
            }
            var clip = AssetManager.Instance.GetAsset<AudioClip>(key);

            if (crossFade == false)
            {
                _bgmAudioSource.clip = clip;
                _bgmAudioSource.volume = volume;
                _bgmAudioSource.Play();
                _lastBgmKey = key;
                return;
            }
            await CrossFadeBgm(clip, volume, fadeDuration);
            _lastBgmKey = key;
        }

        private async UniTask CrossFadeBgm(AudioClip clip, float volume = 1f, float fadeTime = 0.2f)
        {
            var newSource = GetAudioSource();
            newSource.loop = true;
            newSource.volume = 0;
            newSource.spatialBlend = 0;
            newSource.clip = clip;
            newSource.Play();
            float elapsedTime = 0;
            float startActiveVolume = _bgmAudioSource.volume;
            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float t = elapsedTime / fadeTime;
                _bgmAudioSource.volume = Mathf.Lerp(startActiveVolume, 0, t);
                newSource.volume = Mathf.Lerp(0, volume, t);
                await UniTaskHelper.Yield(this);
            }
            ReturnAudioSource(_bgmAudioSource);
            newSource.volume = volume;
            _bgmAudioSource = newSource;
        }

        public async UniTask StopBGM(float fadeDuration = 0.2f)
        {
            if (_bgmAudioSource == null) return;
            float elapsedTime = 0;
            float startActiveVolume = _bgmAudioSource.volume;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float t = elapsedTime / fadeDuration;
                _bgmAudioSource.volume = Mathf.Lerp(startActiveVolume, 0, t);
                await UniTaskHelper.Yield(this);
            }
            _bgmAudioSource.volume = 0;
            ReturnAudioSource(_bgmAudioSource);
            _bgmAudioSource = null;
        }

        public AudioSourceWrapper PlayLoop(string key, float volume = 1, float spatialBlend = 0)
        {
            var clip = AssetManager.Instance.GetAsset<AudioClip>(key);
            var source = GetAudioSource();
            source.spatialBlend = spatialBlend;
            source.loop = true;
            source.clip = clip;
            source.SetOutputMixerGroup(_bgmGroup);
            source.Play();
            _activeAudioSource.Add(source);
            return source;
        }

        private AudioSourceWrapper GetAudioSource()
        {
            AudioSourceWrapper source = null;
            if (_audioSourcePool.Count > 0)
            {
                source = _audioSourcePool.Pop();
                source.transform.SetParent(null);
                source.gameObject.SetActive(true);
            }
            else
            {
                var sourceObject = new GameObject("AudioSource").AddComponent<AudioSource>();
                source = new AudioSourceWrapper(sourceObject);
                source.minDistance = 4f;
                source.maxDistance = 50f;
                Object.DontDestroyOnLoad(source.gameObject);
            }

            source.loop = false;
            source.spatialBlend = 0;
            source.volume = 1;
            source.SetOutputMixerGroup(_sfxGroup);
            
            return source;
        }
        
        private void ReturnAudioSource(AudioSourceWrapper source)
        {
            source.Stop();
            source.clip = null;
            source.gameObject.SetActive(false);
            source.transform.SetParent(_audioSourceRoot);
            _audioSourcePool.Push(source);
        }

        public void SetSfxGroupVolume(float value) => _masterMixer.SetFloat("SFXVolume", LinearToDb(value));
        public void SetBgmGroupVolume(float value) => _masterMixer.SetFloat("BGMVolume", LinearToDb(value));

        public async UniTask ProgressBgmGroupVolume(float targetVolume, float duration, ICancelable cancelable = null)
        {
            if (duration <= 0f)
            {
                SetBgmGroupVolume(targetVolume);
                return;
            }
            var elapsed = 0f;
            var startVolume = GetBgmGroupVolume();
            while (elapsed < duration)
            {
                await UniTaskHelper.Yield(cancelable ?? this);
                elapsed += Time.deltaTime;
                SetBgmGroupVolume(Mathf.Lerp(startVolume, targetVolume, elapsed / duration));
            }
            SetBgmGroupVolume(targetVolume);
        }

        public float GetBgmGroupVolume()
        {
            _masterMixer.GetFloat("BGMVolume", out float db);
            return DbToLinear(db);
        }

        public float GetSfxGroupVolume()
        {
            _masterMixer.GetFloat("SFXVolume", out float db);
            return DbToLinear(db);
        }

        private static float LinearToDb(float value) => value <= 0 ? -80f : Mathf.Log10(value) * 20f;
        private static float DbToLinear(float db) => Mathf.Pow(10f, db / 20f);

        public void Register()
        {
            GameLoop.RegisterUpdate(this);
        }

        public void Unregister()
        {
            GameLoop.UnregisterUpdate(this);
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

        
        public bool IsValidCancelToken()
        {
            return _tokenSource is { IsCancellationRequested: false };
        }
    }
}
