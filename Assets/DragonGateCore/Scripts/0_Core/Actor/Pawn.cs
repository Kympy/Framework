using System;
using UnityEngine;

namespace DragonGate
{
    /// <summary>
    /// 조작 가능한 월드 상의 오브젝트. 컨트롤러를 통해 조작 가능 상태로 전환이 가능하다.
    /// </summary>
    public abstract class Pawn : Actor
    {
        [Header("Movement")]
        [SerializeField] protected float _walkSpeed = 1f;
        [SerializeField] protected float _runSpeed = 2f;
        [SerializeField] protected float _acceleration = 20f;
        [Header("Rotation")]
        [SerializeField] protected float _rotationLerpSpeed = 10f;

        public bool HasMovement => _currentMoveDirection.sqrMagnitude.IsZero() == false;
        
        protected TPlayerController GetOwnerController<TPlayerController>() where TPlayerController : PlayerController => _ownerController as TPlayerController;
        private PlayerController _ownerController;

        protected Vector3? _movementDestination = null; // 이동 목표 지점
        protected float _maxSpeed = 0; // 목표치로 정한 이동속도
        protected float _currentMoveSpeed = 0;
        protected Vector3 _currentMoveDirection = Vector3.zero;

        public override void Init()
        {
            base.Init();
            _currentMoveSpeed = _walkSpeed;
            _maxSpeed = _walkSpeed;
        }

        private void Start()
        {
            GameLoop.RegisterFixed(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameLoop.UnregisterFixed(this);
        }

        public virtual void OnPossess(PlayerController controller)
        {
            _ownerController = controller;
        }

        public virtual void OnUnPossess()
        {
            _ownerController = null;
        }

        public override void OnFixedUpdate(float dt)
        {
            base.OnFixedUpdate(dt);
        }

        public override void SetVelocity(Vector3 velocity)
        {
            var targetSpeed = HasMovement ? _maxSpeed : 0;
            _currentMoveSpeed = Mathf.MoveTowards(_currentMoveSpeed, targetSpeed, _acceleration * Time.deltaTime);
            
            base.SetVelocity(velocity);
        }

        public virtual void SetMovementDirection(Vector3 direction)
        {
            _currentMoveDirection = direction;
        }

        public virtual void StopMovement()
        {
            _movementDestination = null;
            SetMovementDirection(Vector3.zero);
        }

        public virtual void OnMovementInput(Vector3 inputAxis)
        {
            _movementDestination = null;
            SetMovementDirection(inputAxis);
        }
        
        // 월드 포지션까지 이동을 명령 받았을 때
        public virtual void OnReceiveClickPosition(Vector2 clickPosition)
        {
            if (CameraManager.TryGetScreenToWorldPosition(clickPosition, out var worldPosition) == false)
            {
                return;
            }
            _movementDestination = worldPosition;
        }

        public bool TryMoveToDestination()
        {
            if (_movementDestination == null) return false;
             
            var currentPosition = _rigidBody != null ? _rigidBody.position : transform.position;
            var directionVector = _movementDestination.Value - currentPosition;
            directionVector.y = 0;
            if (directionVector.sqrMagnitude.IsLessThan(0.1f))
            {
                StopMovement();
                return false;
            }
            SetMovementDirection(directionVector.normalized);
            return true;
        }

        public virtual void LookLerp(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(direction), _rotationLerpSpeed * Time.deltaTime);
        }

        public virtual void SetRun()
        {
            _maxSpeed = _runSpeed;
        }

        public virtual void SetWalk()
        {
            _maxSpeed = _walkSpeed;
        }
    }
}