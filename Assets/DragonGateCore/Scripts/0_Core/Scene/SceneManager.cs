using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace DragonGate
{
    public partial class SceneManager : Singleton<SceneManager>, ICancelable
    {
        public SceneBase CurrentScene { get; private set; }
        public T GetScene<T>() where T : SceneBase { return CurrentScene as T; }
        // 중복 로드 방지를 위한 플래그
        private bool _pending = false;
        private CancellationTokenSource _tokenSource;

        protected override void OnCreate()
        {
            base.OnCreate();
            GetTokenSource();
        }

        public void LoadScene(SceneInfo sceneInfo)
        {
            if (_pending)
            {
                DGDebug.LogError("SceneManager: LoadScene() has been called more than once");
                return;
            }
            _pending = true;
            ShowLoadingScreen(sceneInfo.LoadingScreenReference?.RuntimeKey?.ToString(), () => LoadSceneInternal(sceneInfo).Forget());
        }

        private async UniTask LoadSceneInternal(SceneInfo sceneInfo)
        {
            if (CurrentScene != null)
            {
                CurrentScene.Exit();
                CurrentScene = null;
            }
            
            var sceneKey = sceneInfo.SceneReference.RuntimeKey;
            if (sceneKey == null)
            {
                DGDebug.LogError("SceneManager: LoadSceneInternal(): sceneKey is null");
                return;
            }
            
            DGDebug.Log($"Load Scene : {sceneInfo.SceneReference.AssetGUID}");
            _ = GetTokenSource();
            
            await Addressables.LoadSceneAsync(sceneKey, LoadSceneMode.Single);
            if (IsValidCancelToken() == false)
            {
                DGDebug.Log("Scene Loading Canceled.", Color.deepPink);
                return;
            }
            // 중요. 씬로드 어드레서블 콜백에서 벗어나기 위한 처리
            await UniTaskHelper.NextFrame(this);

            var sceneBase = Object.FindFirstObjectByType<SceneBase>();
            if (sceneBase == null)
            {
                CurrentScene = null;
            }
            else
            {
                CurrentScene = sceneBase;
                CurrentScene.SceneInfo = sceneInfo;
                await CurrentScene.OnSceneLoaded();
            }
            HideLoadingScreen();
            if (CurrentScene != null)
                CurrentScene.OnSceneEnter();
            _pending = false;
        }

        public CancellationTokenSource GetTokenSource()
        {
            if (_tokenSource == null || _tokenSource.IsCancellationRequested)
            {
                _tokenSource = UniTaskHelper.CreateSceneToken();
            }
            return _tokenSource;
        }

        public void CancelToken()
        {
            _tokenSource?.Cancel();
            _tokenSource?.Dispose();
            _tokenSource = null;
        }

        public bool IsValidCancelToken()
        {
            return _tokenSource is { IsCancellationRequested: false };
        }
    }
}
