using UnityEngine;
using UnityEngine.InputSystem;

namespace DragonGate
{
    [RequireComponent(typeof(Camera))]
    public class RTSCamera : CameraBase<RTSCamera>
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
        [SerializeField] private InputAction _dragRotateDeltaAction;   // e.g. <Mouse>/delta
        [SerializeField] private InputAction _dragRotateButtonAction;  // e.g. <Mouse>/rightButton

        private Vector3 _pivotPosition = Vector3.zero;

        protected override void OnEnable()
        {
            base.OnEnable();
            _moveAction.Enable(); _rotateAction.Enable();
            _dragRotateDeltaAction?.Enable();
            _dragRotateButtonAction?.Enable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _moveAction.Disable(); _rotateAction.Disable();
            _dragRotateDeltaAction?.Disable();
            _dragRotateButtonAction?.Disable();
        }

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
                float rotateInput = GetRotateInput();
                _yaw -= rotateInput * deltaTime * _rotateSpeed;
            }

            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            var desiredPosition = _pivotPosition + rotation * new Vector3(0, 0, -_distance);

            if (_lerp == false)
            {
                transform.position = desiredPosition;
                transform.rotation = rotation;
                return;
            }

            // rotation만 Slerp → position은 보간된 rotation 기준으로 계산
            // position을 독립적으로 Lerp하면 직선으로 이동해 피벗 중심 공전이 깨짐
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, deltaTime * _lerpSpeed);
            transform.position = _pivotPosition + transform.rotation * new Vector3(0, 0, -_distance);
        }

        // 사용에 따라 상속받아 Input 방식 재정의
        protected virtual Vector3 GetAxisInput()
        {
            var move = _moveAction.ReadValue<Vector2>();
            return new Vector3(move.x, 0f, move.y);
        }

        // ex ) Q: -1, E: +1
        protected float GetRotateInput()
        {
            float rotateValue = 0f;

            // 기존 키 입력 (Q/E 등)
            rotateValue += _rotateAction.ReadValue<float>();

            // 클릭 드래그 회전
            if (_dragRotateButtonAction != null && _dragRotateButtonAction.IsPressed())
            {
                if (_dragRotateDeltaAction != null)
                {
                    Vector2 delta = _dragRotateDeltaAction.ReadValue<Vector2>();
                    rotateValue += -delta.x; // 좌우 드래그 → Yaw 회전
                }
            }

            return rotateValue;
        }

        public void Shake(float duration, Vector3 strength, bool ignoreTimescale = false)
        {
            _lockPosition = true;
            _lockZoom = true;
            _camera.Shake(duration, strength, ignoreTimescale).onComplete = () =>
            {
                _lockPosition = false;
                _lockZoom = false;
            };
        }
    }
}