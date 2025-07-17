using UnityEngine;

namespace Framework
{
    public partial class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Vector3 _offset = new Vector3(0, 2.2f, 0);
        [SerializeField] private float _distance = 6f;
        [SerializeField] private float _sensitivity = 3f;
        [SerializeField] private float _pitchMin = -30f;
        [SerializeField] private float _pitchMax = 80f;
        [SerializeField] private float _defaultPitch = 40f;
        [SerializeField] private LayerMask _obstacleLayerMask = ~0;
        [SerializeField] private float _cameraBodySize = 0.25f;

        [SerializeField] private Transform _followTarget;

        private float _yaw;
        private float _pitch;

        private void Awake()
        {
            _pitch = _defaultPitch;
        }

        private void Update()
        {
            if (_followTarget == null) return;

            _yaw += Input.GetAxis("Mouse X") * _sensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * _sensitivity;
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

            _camera.transform.position = pivot + rotation * new Vector3(0, 0, -hitDistance);
            transform.LookAt(pivot);
        }
    }
}