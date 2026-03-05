using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DragonGate
{
    [RequireComponent(typeof(Camera))]
    public abstract class CameraBase<T> : PlacedMonoBehaviourSingleton<T>, IInputHandler where T : MonoBehaviour
    {
        [SerializeField, CanBeNull] protected Transform _followTarget;

        [Header("Settings")]
        [SerializeField]  protected bool _ignoreTimeScale = true;
        [SerializeField] protected bool _lerp = true;
        [Header("Position")]
        [SerializeField] protected Vector3 _offset = new Vector3(0, 2.2f, 0);
        [SerializeField] protected float _distance = 10f;
        [SerializeField] protected float _minDistance = 3f;
        [SerializeField] protected float _maxDistance = 15f;
        [Header("Speed")]
        [SerializeField] protected float _sensitivity = 3f;
        [SerializeField] protected float _lerpSpeed = 10f;
        [Header("Zoom")]
        [SerializeField] protected float _zoomInputSensitivity = 10f;
        [SerializeField] protected InputAction _zoomAction;
        
        [Header("Collision")]
        [SerializeField] protected LayerMask _obstacleLayerMask = ~0;
        [SerializeField] protected float _cameraBodySize = 0.25f;
        
        protected Camera _camera;
        protected bool _lockPosition = false; // 카메라 움직임을 고정시킴
        protected bool _lockZoom = false;

        private float _zoomTargetDistance;
        private float _zoomMoveSpeed = 5f;
        private bool _isZoomMoving = false;

        protected override void Awake()
        {
            base.Awake();
            _camera = GetComponent<Camera>();
        }

        protected virtual void OnEnable()
        {
            _zoomAction.Enable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _zoomAction.Disable();
        }

        protected virtual void Start()
        {
            InitCamera();
        }

        protected virtual void OnDestroy() { }
        
        public EInputResult UpdateInput(float deltaTime)
        {
            if (_lockPosition) return EInputResult.Continue;
            float dt = _ignoreTimeScale ? Time.unscaledDeltaTime : deltaTime;
            UpdateCamera(dt);
            UpdateZoom(dt);
            return EInputResult.Continue;
        }

        protected virtual void UpdateCamera(float deltaTime) { }

        public void SetPositionLock(bool locked)
        {
            _lockPosition = locked;
        }

        public void SetZoomLock(bool locked)
        {
            _lockZoom = locked;
        }

        public virtual void InitCamera()
        {
            // 초기 세팅을 잠시 저장하고, 보간 없이 카메라 위치 세팅
            var lerpValue = _lerp;
            _lerp = false;
            var deltaTime = _ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
                UpdateCamera(deltaTime);
            _lerp = lerpValue;
        }

        public virtual void SetFollowTarget(Transform followTarget)
        {
            _followTarget = followTarget;
        }

        public virtual void UpdateZoom(float deltaTime)
        {
            if (_lockZoom) return;
            
            if (_isZoomMoving)
            {
                _distance = Mathf.MoveTowards(_distance, _zoomTargetDistance, _zoomMoveSpeed * deltaTime);

                if (Mathf.Approximately(_distance, _zoomTargetDistance))
                {
                    _distance = _zoomTargetDistance;
                    _isZoomMoving = false;
                }
                return;
            }
            
            if (Application.isFocused == false || InputManager.IsPointerOutsideGameWindow()) return;
            if (InputManager.IsPointerOverUI()) return;
            
            var zoomValue = _zoomAction.ReadValue<float>();
            _distance -= zoomValue * deltaTime * _zoomInputSensitivity;
            _distance = Mathf.Clamp(_distance, _minDistance, _maxDistance);
        }

        public virtual void SetZoom(float distance)
        {
            _isZoomMoving = false;
            _distance = Mathf.Clamp(distance, _minDistance, _maxDistance);
            _zoomTargetDistance = _distance;
        }

        public void SetZoomMax()
        {
            SetZoom(_maxDistance);
        }

        public void SetZoomMin()
        {
            SetZoom(_minDistance);
        }

        public void SetZoomAverage()
        {
            SetZoom((_minDistance + _maxDistance) / 2f);
        }

        public virtual void Zoom(float targetDistance, float speed)
        {
            _zoomTargetDistance = Mathf.Clamp(targetDistance, _minDistance, _maxDistance);
            _zoomMoveSpeed = speed;
            _isZoomMoving = true;
        }

        public void ZoomToAverage(float speed = -1)
        {
            if (speed < 0)
            {
                speed = _zoomMoveSpeed;
            }
            Zoom((_minDistance + _maxDistance) / 2f, speed);
        }
    }
}
