using UnityEngine;

namespace Framework
{
    public partial class ThirdPersonCamera : MonoBehaviour
    {
        protected float currentHorizontalValue = 0;
        protected float currentVerticalValue = 0;

        [SerializeField] protected float distance = 5f;
        [SerializeField] protected float sensitivity = 2f;

        private RaycastHit[] hitObjectsBuffer;
        private const int maxHitCheck = 20;

        // protected override void Awake()
        // {
        //     base.Awake();
        //
        //     bLookTarget = true;
        // }

        private void Start()
        {
            // hitObjectsBuffer = ArrayPoolWrapper<RaycastHit>.Rent(maxHitCheck, gameObject.name);
        }

        private void OnDestroy()
        {
            // ArrayPoolWrapper<RaycastHit>.Return(hitObjectsBuffer, true);
        }

        public void OnUpdate(float deltaTime)
        {
            FollowTarget();
        }

        // protected override void FollowTarget()
        // {
        //     // 마우스 움직임에 따라 카메라 회전
        //     currentHorizontalValue += Input.GetAxis(Constants.Mouse_X) * sensitivity * Time.deltaTime;
        //     currentVerticalValue -= Input.GetAxis(Constants.Mouse_Y) * sensitivity * Time.deltaTime;
        //
        //     // 구면 좌표를 직교 좌표계로 변환
        //     float radTheta = Mathf.Deg2Rad * currentHorizontalValue;
        //     float radPhi = Mathf.Deg2Rad * currentVerticalValue;
        //
        //     float x = distance * Mathf.Cos(radPhi) * Mathf.Sin(radTheta);
        //     float y = distance * Mathf.Sin(radPhi);
        //     float z = distance * Mathf.Cos(radPhi) * Mathf.Cos(radTheta);
        //
        //     Vector3 lookPoint = followTarget.position + locationOffset;
        //     Vector3 targetPosition = lookPoint + new Vector3(x, y, z);
        //
        //     Vector3 lookDirection = lookPoint - targetPosition;
        //     lookDirection.Normalize();
        //
        //     Ray ray = new Ray(targetPosition, lookDirection);
        //
        //     // Debug.DrawRay(ray.origin, ray.direction * distance, Color.red);
        //     Array.Clear(hitObjectsBuffer, 0, hitObjectsBuffer.Length);
        //     int hitCount = Physics.RaycastNonAlloc(ray, hitObjectsBuffer, 3f);
        //     if (hitCount > 0)
        //     {
        //         // 가장 마지막에 부딪힌 것
        //         targetPosition = hitObjectsBuffer[hitCount - 1].point;
        //         Debug.Log(hitObjectsBuffer[hitCount - 1].transform.gameObject.name);
        //     }
        //
        //     DGLog.DrawLine(targetPosition, lookPoint, Color.red);
        //
        //     // 카메라 위치 설정
        //     transform.position = targetPosition;
        //     transform.LookAt(lookPoint); // 카메라가 항상 플레이어를 바라보게 함
        // }
        //
        // public bool IgnorePause()
        // {
        //     return false;
        // }
        //
        // public void Register()
        // {
        //     UpdateManager.Instance.Register(this);
        // }
        //
        // public void UnRegister()
        // {
        //     UpdateManager.Instance.UnRegister(this);
        // }
    }
}