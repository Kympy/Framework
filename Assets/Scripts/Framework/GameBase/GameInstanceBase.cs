using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Framework
{
    public abstract class GameInstanceBase : MonoBehaviourSingleton<GameInstanceBase>
    {
        [SerializeField] protected string _startScene;

        private World _worldContext;
        private AsyncOperationHandle<SceneInstance> _sceneHandle;

        public virtual void InitGame()
        {
            OpenScene(_startScene, LoadSceneMode.Single, true);
        }

        public virtual void StartGame()
        {
            
        }

        public virtual void DestroyGame()
        {
            
        }
        
        public World GetWorld()
        {
            return _worldContext;
        }

        public void OpenScene(string key, LoadSceneMode mode, bool activateOnLoad = true)
        {
            if (_sceneHandle.IsValid())
            {
                Addressables.Release(_sceneHandle);
            }
            _sceneHandle = Addressables.LoadSceneAsync(key, mode, activateOnLoad);
            _sceneHandle.WaitForCompletion();
            _worldContext = FindWorldContext();
            _worldContext.InitWorld();
        }

        private World FindWorldContext()
        {
            World worldContext = FindFirstObjectByType<World>();
            if (worldContext == null)
            {
                throw new System.Exception("World context not found");
            }
            return worldContext;
        }
    }
}