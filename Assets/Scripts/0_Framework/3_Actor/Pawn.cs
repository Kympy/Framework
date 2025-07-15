namespace Framework
{
    public class Pawn : EngineObject
    {
        protected PlayerControllerBase _playerController;

        public virtual void PossessedBy(PlayerControllerBase playerController)
        {
            _playerController = playerController;
        }

        public virtual void UnPossessed()
        {
            
        }
    }
}
