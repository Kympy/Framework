using System;
using JetBrains.Annotations;
using UnityEngine;

namespace DragonGate
{
    [RequireComponent(typeof(Camera))]
    public abstract class CameraBase : MonoBehaviour, GameLoop.IGameUpdate
    {
        [SerializeField, CanBeNull] protected Transform _followTarget;

        [Space, Header("Settings")]
        [SerializeField]  protected bool _ignoreTimeScale = true;
        [SerializeField] protected Vector3 _offset = new Vector3(0, 2.2f, 0);
        [SerializeField] protected float _distance = 10f;
        [SerializeField] protected float _sensitivity = 3f;
        [SerializeField] protected bool _lerp = true;
        [SerializeField] protected float _lerpSpeed = 10f;
        [Space, Header("Collision")]
        [SerializeField] protected LayerMask _obstacleLayerMask = ~0;
        [SerializeField] protected float _cameraBodySize = 0.25f;
        
        public bool IgnoreTimeScale => _ignoreTimeScale;
        
        protected Camera _camera;
        protected bool _locked = false; // 카메라 움직임을 고정시킴

        protected virtual void Awake()
        {
            _camera = GetComponent<Camera>();
            GameLoop.RegisterUpdate(this);
        }

        protected virtual void Start()
        {
            InitCamera();
        }

        protected virtual void OnDestroy()
        {
            GameLoop.UnregisterUpdate(this);
        }

        public void OnUpdate(float deltaTime)
        {
            if (_locked) return;
            UpdateCamera(deltaTime);
        }

        protected virtual void UpdateCamera(float deltaTime) { }

        public void SetLock(bool locked)
        {
            _locked = locked;
        }

        protected virtual void InitCamera()
        {
            // 초기 세팅을 잠시 저장하고, 보간 없이 카메라 위치 세팅
            var lerpValue = _lerp;
            _lerp = false;
            var deltaTime = _ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
                UpdateCamera(deltaTime);
            _lerp = lerpValue;
        }
    }
}
