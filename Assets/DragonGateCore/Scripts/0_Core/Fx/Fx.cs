using UnityEngine;

namespace DragonGate
{
    public class Fx : MonoBehaviour
    {
        // 모든 배열을 들고 있을 필요 없이, 메인 시스템 하나만 참조
        [SerializeField] protected ParticleSystem _mainParticleSystem;
        [SerializeField] protected bool _autoReturn = true;
        
        public bool IsAlive() => _mainParticleSystem != null && _mainParticleSystem.IsAlive();
        
        protected virtual void Awake()
        {
            // 최상위에 컴포넌트가 있다면 그것을, 없다면 첫 번째 자식을 가져옵니다.
            _mainParticleSystem = GetComponent<ParticleSystem>();
            if (_mainParticleSystem == null)
            {
                _mainParticleSystem = GetComponentInChildren<ParticleSystem>();
            }
        }

        private void Update()
        {
            if (!_autoReturn || _mainParticleSystem == null) return;

            // IsAlive(true)가 기본값이므로, 자식들까지 한 번에 체크합니다.
            if (!_mainParticleSystem.IsAlive())
            {
                ReturnToPool();
            }
        }

        public void SetViewportPosition2D(Vector2 viewportPosition)
        {
            var position = CameraManager.CurrentCamera.ViewportToWorldPoint(viewportPosition);
            position.z = 0;
            transform.position = position;
        }

        public void SetRotation(Vector3 eulerAngles)
        {
            transform.rotation = Quaternion.Euler(eulerAngles);
        }

        private void ReturnToPool()
        {
            PoolManager.Instance?.ReturnFx(this);
        }
    }
}
