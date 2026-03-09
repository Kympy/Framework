using System;
using UnityEngine;

namespace DragonGate
{
    /// <summary>
    /// 어떤 Pawn을 조작하기 위한 클래스. 상속받아 다양한 조작을 구현할 수 있음.
    /// </summary>
    public abstract class PlayerController : PlacedMonoBehaviourSingleton<PlayerController>, GameLoop.IGameUpdate, GameLoop.IGameFixedUpdate, IInputHandler
    {
        public Pawn GetPawn() => _pawn;
        public T GetPawn<T>() where T : Pawn => _pawn as T;
        public bool IgnoreTimeScale { get; } = false;

        protected PlayerControllerType _controllerType = PlayerControllerType.None;
        
        private Pawn _pawn;

        protected virtual void Start()
        {
            RegisterInput();
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnregisterInput();
        }

        public virtual void Possess(Pawn pawn)
        {
            UnPossess();
            _pawn = pawn;
            _pawn.OnPossess(this);
            DGDebug.Log($"Possess Pawn : {pawn.gameObject.name}");
        }

        public virtual void UnPossess()
        {
            if (_pawn == null) return;
            _pawn.OnUnPossess();
            DGDebug.Log($"UnPossess Pawn : {_pawn.gameObject.name}");
            _pawn = null;
        }

        public virtual void OnUpdate(float deltaTime)
        {
            
        }

        public virtual void OnFixedUpdate(float dt)
        {
            
        }

        public bool InputEnabled => gameObject.activeSelf;

        public virtual EInputResult UpdateInput(float deltaTime)
        {
            return EInputResult.Continue;
        }

        public void RegisterInput()
        {
            InputManager.AddInput(this, InputPriority.Player);
        }

        public void UnregisterInput()
        {
            InputManager.RemoveInput(this);
        }
    }
}