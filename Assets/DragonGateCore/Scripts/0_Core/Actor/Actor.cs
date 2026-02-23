using System;
using UnityEngine;

namespace DragonGate
{
    /// <summary>
    /// 월드에 존재하는 조작 불가한 오브젝트. 스스로 움직임을 가질 수는 있지만 Playable이 아니다.
    /// 월드에 존재할 오브젝트로서 수행할 수 있는 기본 기능들을 정의.
    /// </summary>
    public abstract class Actor : CoreBehaviour, GameLoop.IGameFixedUpdate
    {
        [SerializeField] protected Rigidbody _rigidBody;
        protected float _gravityPower = 9.8f;
        protected Vector3 _gravityDirection = Vector3.down;

        public override void Init()
        {
            base.Init();
        }

        protected virtual void OnDestroy()
        {
            
        }

        public void SetPosition(Vector3 worldPosition)
        {
            _rigidBody.MovePosition(worldPosition);
        }

        public virtual void SetVelocity(Vector3 velocity)
        {
            _rigidBody.linearVelocity = velocity;
        }

        public void AddVelocity(Vector3 velocity)
        {
            _rigidBody.linearVelocity += velocity;
        }

        public Vector3 GetVelocity()
        {
            return _rigidBody.linearVelocity;
        }

        public virtual void OnFixedUpdate(float dt)
        {
            UpdateGravity(dt);
        }

        protected virtual void UpdateGravity(float deltaTime)
        {
            AddVelocity(_gravityDirection * (_gravityPower * deltaTime));
        }

        public virtual void Look(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}