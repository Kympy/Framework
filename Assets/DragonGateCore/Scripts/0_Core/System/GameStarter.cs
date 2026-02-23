using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
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

        private void Start()
        {
            Object.DontDestroyOnLoad(gameObject);
            StartGame().Forget();
        }

        private void OnApplicationQuit()
        {
            IsApplicationQuitting = true;
        }

        private async UniTask StartGame()
        {
            CreateEssentialGlobalSingleton();
            CreateGlobalSingleton();
            OnSingletonCreated();
            
            await PreLoad();
            
            SetApplicationSetting();
            CreateEventSystem();
            
            LoadTargetScene(_targetScene);
        }

        private void CreateEssentialGlobalSingleton()
        {
            GameDataManager.CreateInstance();
            GameLoop.CreateInstanceDontDestroyOnLoad();
            InputManager.CreateInstance();
            AssetManager.CreateInstance();
            SceneManager.CreateInstance();
            UIManager.CreateInstance();
            SoundManager.CreateInstance();
        }

        protected virtual void CreateGlobalSingleton() { }

        protected virtual void OnSingletonCreated()
        {
            
        }

        protected virtual UniTask PreLoad()
        {
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
    }
}