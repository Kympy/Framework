using UnityEngine;
using UnityEngine.InputSystem;

namespace Framework
{
    public class PlayerControllerBase : EngineObject
    {
        private PlayerInputActions _inputActions;
        private Pawn _possessedPawn;

        public virtual void InitController()
        {
            BindInputActions();
        }
        
        public virtual void Possess(Pawn pawn)
        {
            UnPossess();
            _possessedPawn = pawn;
            _possessedPawn.PossessedBy(this);
        }

        public virtual void UnPossess()
        {
            if (_possessedPawn == null) return;
            _possessedPawn.UnPossessed();
            _possessedPawn = null;
        }

        public Pawn GetPawn()
        {
            return _possessedPawn;
        }

        protected virtual void BindInputActions()
        {
            _inputActions = new PlayerInputActions();
            SetInputMode(EInputMode.Player);
            _inputActions.Player.Move.performed += AddMovementInput;
            _inputActions.Player.Jump.performed += Jump;
        }
        
        public void SetInputMode(EInputMode inputMode)
        {
            if (_inputActions == null) return;
            
            foreach (var map in _inputActions.asset.actionMaps)
            {
                map.Disable();
            }

            switch (inputMode)
            {
                case EInputMode.Player:
                    _inputActions.Player.Enable();
                    break;
                case EInputMode.UI:
                    _inputActions.UI.Enable();
                    break;
            }
        }

        protected virtual void AddMovementInput(InputAction.CallbackContext context)
        {
            Vector2 input = context.ReadValue<Vector2>();
            GetPawn().AddMovementInput(input);
        }

        protected virtual void Jump(InputAction.CallbackContext context)
        {
            
        }
    }
}
