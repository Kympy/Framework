using UnityEngine;

namespace Framework
{
    [System.Serializable]
    public class GameModeBase : EngineObject
    {
        [SerializeField] protected Camera _mainCamera;
        [SerializeField] protected PlayerControllerBase _playerController;
        [SerializeField] protected HUDBase _hud;

        public virtual void InitMode()
        {
            
        }
        
        public override void SetWorldContext(World worldContext)
        {
            base.SetWorldContext(worldContext);
            _playerController.SetWorldContext(worldContext);
        }

        public virtual void SpawnPlayer()
        {
            GetWorld().SpawnObject<Actor>("asdf");
        }
    }
}