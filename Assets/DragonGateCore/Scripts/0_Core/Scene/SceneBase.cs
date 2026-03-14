using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DragonGate
{
    [DefaultExecutionOrder(0)]
    public abstract class SceneBase : CoreBehaviour
    {
        [HideInInspector] public SceneInfo SceneInfo;
        [SerializeField] protected PlayerController _playerControllerResource;
        [SerializeField] protected Pawn _defaultPawn;

        private PlayerController _playerController;
        private Pawn _pawn;
        
        public PlayerController GetPlayerController() => _playerController;
        
        // 씬 로드 완료 후 Pre-Load가 필요한 부분들을 작성. 실제 씬이 눈에 보여지기 전 시점.
        public virtual async UniTask OnSceneLoaded()
        {
            CameraManager.EnableCamera(Camera.main);
            CreateSceneSingleton();
            CreatePlayerController();
            await CreatePawn();
            await PreLoad();
        }
        // 사전에 미리 로드해야하거나 Warm up 이 필요한 부분을 여기에 작성
        protected virtual UniTask PreLoad() { return UniTask.CompletedTask; }
        
        // 씬이 실제로 보여지는 시점. 씬 입장 이후에 대한 처리
        public virtual void OnSceneEnter()
        {
            DGDebug.Log($"Scene Enter : {SceneInfo.SceneReference.RuntimeKey.ToString()}");
            
            if (_pawn != null && _playerController != null)
            {
                _playerController.Possess(_pawn);
            }
        }

        public void Exit()
        {
            DGDebug.Log($"Destroy Scene : {SceneInfo.SceneReference.RuntimeKey.ToString()}");
            OnDestroy();
            CancelToken();
        }

        protected virtual void OnDestroy()
        {
            DestroySceneSingleton();
        }

        protected virtual void CreateSceneSingleton() { }
        protected virtual void DestroySceneSingleton() { }

        protected virtual void CreatePlayerController()
        {
            if (_playerControllerResource != null)
            {
                _playerController = Instantiate(_playerControllerResource);
            }
        }

        protected async UniTask CreatePawn()
        {
            if (_defaultPawn == null)
            {
                DGDebug.Log("Default Pawn is not exists", Color.aliceBlue);
                return;
            }
            _pawn = Instantiate(_defaultPawn);
            _pawn.Init();
            await OnCreatePawn(_pawn);
        }

        protected virtual UniTask OnCreatePawn(Pawn pawn)
        {
            return UniTask.CompletedTask;
        }
    }
}
