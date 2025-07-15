using UnityEngine;

namespace Framework
{
    public class PlayerControllerBase : EngineObject
    {
        protected PlayerInputActions _inputActions;
        protected Pawn _possessedPawn;

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

        protected virtual void BindInputActions()
        {
            _inputActions = new PlayerInputActions();
            SetInputMode(EInputMode.Player);
            // _inputActions.Player.Move.performed += something;
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
    }
}
