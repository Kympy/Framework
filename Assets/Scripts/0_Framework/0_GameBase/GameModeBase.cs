using UnityEngine;

namespace Framework
{
    public class GameModeBase : EngineObject
    {
        [SerializeField] protected Camera _mainCamera;
        [SerializeField] protected Pawn _defaultPawnPrefab;
        [SerializeField] protected PlayerControllerBase _playerControllerPrefab;
        [SerializeField] protected HUD _hudPrefab;
        
        private PlayerControllerBase _playerController;
        private HUD _hud;

        public virtual void InitMode()
        {
            DGLog.Log(GetType(), "Initializing Game Mode...", Color.yellow);
            SpawnPlayerController();
            SpawnHUD();
        }

        protected virtual void SpawnPlayerController()
        {
            if (_playerControllerPrefab == null)
            {
                DGLog.LogWarning("PlayerControllerPrefab is not set.");
                return;
            }
            DGLog.Log(GetType(), "Spawning Player Controller...", Color.yellow);
            var playerController = GetWorld().SpawnObject(_playerControllerPrefab);
            _playerController = playerController;
            _playerController.InitController();
        }

        protected virtual void SpawnHUD()
        {
            if (_hudPrefab == null)
            {
                DGLog.LogWarning("HUDPrefab is not set.");
                return;
            }
            DGLog.Log(GetType(), "Spawning HUD...", Color.yellow);
            var hud = GetWorld().SpawnObject(_hudPrefab);
            _hud = hud;
            _hud.InitHUD();
        }

        protected virtual void CreatePawn()
        {
            if (_defaultPawnPrefab == null) return;
            var pawn = GetWorld().SpawnObject(_defaultPawnPrefab);
            if (_playerController != null)
            {
                _playerController.Possess(pawn);
            }
        }
    }
}