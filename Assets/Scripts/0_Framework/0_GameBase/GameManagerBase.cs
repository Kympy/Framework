using UnityEngine;
using UnityEngine.Serialization;

namespace Framework
{
    public abstract class GameManagerBase : MonoBehaviourSingleton<GameManagerBase>
    {
        [SerializeField] private GameInstanceBase _gameInstance;
        
        private float _gameTimeScale = 1f;
        
        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
            EssentialAwake();
            Initialize();
        }

        protected virtual void Initialize()
        {
            if (_gameInstance == null)
                throw new System.Exception("Game Instance is not allocated.");
            var gameInstanceObject = Instantiate(_gameInstance);
            DontDestroyOnLoad(gameInstanceObject);
            gameInstanceObject.TryGetComponent(out GameInstanceBase gameInstanceBase);
            gameInstanceBase.InitGame();
        }

        protected virtual void OnApplicationPause(bool pauseStatus)
        {
            
        }

        protected virtual void OnApplicationQuit()
        {
            
        }

        protected virtual void EssentialAwake()
        {
            TickManager.CreateInstance();
            GameOption.CreateInstance();
            AssetManager.CreateInstance();
            SoundManager.CreateInstance();
            
            Application.lowMemory -= OnLowMemory;
            Application.lowMemory += OnLowMemory;
        }

        protected virtual void LoadStartLevel()
        {
            
        }

        public void SetTimeScale(float timeScale)
        {
            _gameTimeScale = timeScale;
        }

        public void PauseGame()
        {
            Time.timeScale = 0;
        }

        public void ResumeGame()
        {
            Time.timeScale = _gameTimeScale;
        }

        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnLowMemory()
        {
            if (AssetManager.HasInstance)
            {
                AssetManager.Instance.ReleaseUnReferencedAssets();
            }
        }
    }
}
