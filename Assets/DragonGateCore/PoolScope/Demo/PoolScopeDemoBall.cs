using DragonGate;
using UnityEngine;

namespace DragonGate
{
    [RequireComponent(typeof(Rigidbody))]
    public class PoolScopeDemoBall : MonoBehaviour
    {
        private Rigidbody _rigidbody;
        private float _bornTime;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = true;
            _rigidbody.isKinematic = false;
        }

        private void OnEnable()
        {
            _bornTime = Time.time;
        }

        private void Update()
        {
            if (Time.time - _bornTime >= 3f)
            {
                PoolScope.Return(this);
            }
        }
    }
}
