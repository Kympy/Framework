using UnityEngine;
using UnityEngine.InputSystem;

namespace DragonGate
{
    [RequireComponent(typeof(Camera))]
    public partial class ThirdPersonCamera : CameraBase
    {
        [Header("Third Person Camera")]
        [SerializeField] private float _pitchMin = -30f;
        [SerializeField] private float _pitchMax = 80f;
        [SerializeField] private float _defaultPitch = 40f;
        [Space, Header("Input")]
        [SerializeField] private InputAction _lookAction;

        private float _yaw;
        private float _pitch;

        protected override void Awake()
        {
            base.Awake();
            _pitch = _defaultPitch;
        }

        private void OnEnable() => _lookAction.Enable();
        private void OnDisable() => _lookAction.Disable();

        protected override void UpdateCamera(float deltaTime)
        {
            if (_followTarget == null) return;

            var look = _lookAction.ReadValue<Vector2>();
            _yaw += look.x * _sensitivity;
            _pitch -= look.y * _sensitivity;
            _pitch = Mathf.Clamp(_pitch, _pitchMin, _pitchMax);

            Vector3 pivot = _followTarget.position + _offset;
            var rotation = Quaternion.Euler(_pitch, _yaw, 0.0f);

            // 카메라 목표 위치 계산
            Vector3 desiredCameraPos = pivot + rotation * new Vector3(0, 0, -_distance);
            DrawLine(pivot, desiredCameraPos, Color.white);
            // 장애물 체크: pivot → desiredCameraPos 방향으로 레이캐스트
            Vector3 direction = desiredCameraPos - pivot;
            float hitDistance = _distance;
            if (Physics.Raycast(pivot, direction.normalized, out RaycastHit hit, _distance, _obstacleLayerMask))
            {
                hitDistance = hit.distance;
                DrawLine(pivot, hit.point, Color.red);
            }
            hitDistance = Mathf.Clamp(hitDistance, _cameraBodySize, _distance);

            transform.position = pivot + rotation * new Vector3(0, 0, -hitDistance);
            transform.LookAt(pivot);
        }
    }
}