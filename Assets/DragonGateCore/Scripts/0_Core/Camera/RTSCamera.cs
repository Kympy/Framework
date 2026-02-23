using UnityEngine;
using UnityEngine.InputSystem;

namespace DragonGate
{
    [RequireComponent(typeof(Camera))]
    public class RTSCamera : CameraBase
    {
        [Header("RTS Camera")]
        [SerializeField] private float _yaw;
        [SerializeField] private float _pitch;

        [Space]
        [SerializeField] private bool _allowRotation = true;
        [SerializeField] private float _rotateSpeed = 90f;
        [Space, Header("Input")]
        [SerializeField] private InputAction _moveAction;
        [SerializeField] private InputAction _rotateAction;

        private Vector3 _pivotPosition = Vector3.zero;

        private void OnEnable() { _moveAction.Enable(); _rotateAction.Enable(); }
        private void OnDisable() { _moveAction.Disable(); _rotateAction.Disable(); }

        protected override void UpdateCamera(float deltaTime)
        {
            base.UpdateCamera(deltaTime);

            if (_followTarget != null)
            {
                _pivotPosition = _followTarget.position;
            }
            else
            {
                var axis = GetAxisInput();

                var forward = transform.forward;
                forward.y = 0;
                forward.Normalize();

                var right = transform.right;
                right.y = 0;
                right.Normalize();

                _pivotPosition += (forward * axis.z + right * axis.x) * (deltaTime * _sensitivity);
            }

            if (_allowRotation)
            {
                _yaw -= GetRotateInput() * deltaTime * _rotateSpeed;
            }

            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            var desiredPosition = _pivotPosition + rotation * new Vector3(0, 0, -_distance);

            if (_lerp == false)
            {
                transform.position = desiredPosition;
                transform.rotation = rotation;
                return;
            }
            transform.position = Vector3.Lerp(transform.position, desiredPosition, deltaTime * _lerpSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, deltaTime * _lerpSpeed);
        }

        // 사용에 따라 상속받아 Input 방식 재정의
        protected virtual Vector3 GetAxisInput()
        {
            var move = _moveAction.ReadValue<Vector2>();
            return new Vector3(move.x, 0f, move.y);
        }

        // ex ) Q: -1, E: +1
        protected virtual float GetRotateInput()
        {
            return _rotateAction.ReadValue<float>();
        }
    }
}