using UnityEngine;

namespace Framework
{
    public class Pawn : EngineObject
    {
        [SerializeField] protected float _velocity = 1f;
        
        protected PlayerControllerBase _playerController;
        protected Vector3 _movementDirection;

        public virtual void PossessedBy(PlayerControllerBase playerController)
        {
            _playerController = playerController;
        }

        public virtual void UnPossessed()
        {
            
        }

        public virtual void AddMovementInput(Vector2 input)
        {
            Vector3 direction = transform.forward * input.y + transform.right * input.x;
            direction.Normalize();
            _movementDirection = direction;
        }

        public virtual void OnJump()
        {
            
        }
    }
}
