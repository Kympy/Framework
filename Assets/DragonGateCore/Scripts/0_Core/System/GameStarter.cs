using System.Globalization;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Object = UnityEngine.Object;

namespace DragonGate
{
    public class GameStarter : MonoBehaviour
    {
        [Header("TargetScene")]
        [SerializeField] private SceneInfo _targetScene;

        [Header("Settings")]
        [SerializeField, CanBeNull] private GameObject _eventSystemPrefab;
        [SerializeField] private int _frameRate = 60;
        
        public static bool IsApplicationQuitting = false;

        protected virtual void Start()
        {
            Object.DontDestroyOnLoad(gameObject);
            StartGame().Forget();
        }

        private void OnApplicationQuit()
        {
            IsApplicationQuitting = true;
        }

        public async UniTask StartGame()
        {
            CreateEssentialGlobalSingleton();
            CreateGlobalSingleton();
            OnSingletonCreated();
            
            await PreLoad();
            
            SetApplicationSetting();
            CreateEventSystem();
            
            OnInitialized();
            
            if (_targetScene != null)
                LoadTargetScene(_targetScene);
        }

        protected virtual void OnInitialized()
        {
            
        }

        private void CreateEssentialGlobalSingleton()
        {
            GameDataManager.CreateInstance();
            GameLoop.CreateInstanceDontDestroyOnLoad();
            InputManager.CreateInstance();
            AssetManager.CreateInstance();
            SceneManager.CreateInstance();
            PoolManager.CreateInstance();
            UIManager.CreateInstance();
            SoundManager.CreateInstance();
        }

        protected virtual void CreateGlobalSingleton() { }

        protected virtual void OnSingletonCreated()
        {
            
        }

        protected virtual UniTask PreLoad()
        {
            DGDebug.Log($"Application System Language : {Application.systemLanguage}");
            DGDebug.Log($"Culture Info Current: {CultureInfo.CurrentCulture}");
            DGDebug.Log($"Culture Info UI : {CultureInfo.CurrentUICulture}");
            if (LocalizationSettings.SelectedLocale == null)
            {
                var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(Application.systemLanguage));
                if (locale != null)
                {
                    LocalizationSettings.SelectedLocale = locale;
                }
            }
            return UniTask.CompletedTask;
        }

        protected virtual void SetApplicationSetting()
        {
            Application.targetFrameRate = _frameRate;
        }

        protected virtual void LoadTargetScene(SceneInfo targetSceneInfo)
        {
            if (targetSceneInfo == null) return;

            SceneManager.Instance.LoadScene(targetSceneInfo);
        }

        protected void CreateEventSystem()
        {
            if (EventSystem.current != null)
                return;
            
            if (_eventSystemPrefab == null)
            {
                var eventSystemGameObject = new GameObject("EventSystem");
                // EventSystem
                var eventSystem = eventSystemGameObject.AddComponent<EventSystem>();
                // Input System UI Module
                var inputModule = eventSystemGameObject.AddComponent<InputSystemUIInputModule>();
                // 선택 사항 (권장)
                eventSystem.firstSelectedGameObject = null;
                DontDestroyOnLoad(eventSystemGameObject);
                return;
            }
            
            var eventSystemObject = Instantiate(_eventSystemPrefab);
            DontDestroyOnLoad(eventSystemObject);
        }

        public static void QuitGame()
        {
        #if UNITY_EDITOR
            if (Application.isEditor && Application.isPlaying)
                UnityEditor.EditorApplication.isPlaying = false;
        #endif
            Application.Quit();
        }
    }
}