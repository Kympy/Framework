using UnityEngine;
using UnityEngine.InputSystem;

namespace DragonGate
{
    public class RaycastHelper
    {
        private static readonly RaycastHit[] _hitBuffer = new RaycastHit[32];

        public static bool TryRaycastFromMouse(
            out RaycastHit hitInfo,
            float maxDistance = 1000f,
            int layerMask = ~0,
            Camera targetCamera = null)
        {
            hitInfo = default;

            Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
            if (cameraToUse == null)
                return false;

            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Ray ray = cameraToUse.ScreenPointToRay(mouseScreenPosition);

            return Physics.Raycast(ray, out hitInfo, maxDistance, layerMask);
        }

        /// <summary>
        /// 마우스 위치에서 레이캐스트하여 모든 후보를 _hitBuffer에 채운 뒤
        /// hits와 hitCount를 반환한다. GC를 발생시키지 않는다.
        /// </summary>
        public static bool TryRaycastAllFromMouse(
            out RaycastHit[] hits,
            out int hitCount,
            float maxDistance = 1000f,
            int layerMask = ~0,
            Camera targetCamera = null)
        {
            hits = _hitBuffer;
            hitCount = 0;

            Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
            if (cameraToUse == null)
                return false;

            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Ray ray = cameraToUse.ScreenPointToRay(mouseScreenPosition);

            hitCount = Physics.RaycastNonAlloc(ray, _hitBuffer, maxDistance, layerMask);
            return hitCount > 0;
        }

        /// <summary>
        /// 마우스 위치에서 모든 후보를 모은 뒤 IRaycastPriority 기준으로 최적 대상을 선정한다.
        /// </summary>
        public static bool TryRaycastFromMouseWithPriority(
            out RaycastHit result,
            float maxDistance = 1000f,
            int layerMask = ~0,
            Camera targetCamera = null)
        {
            result = default;

            if (TryRaycastAllFromMouse(out var hits, out int hitCount, maxDistance, layerMask, targetCamera) == false)
                return false;

            return RaycastPriorityResolver.TryResolve(hits, hitCount, out result);
        }

        public static bool TryGetCameraCenterWorldPosition(
            Camera camera,
            out Vector3 worldPosition,
            float maxDistance = Mathf.Infinity,
            int layerMask = Physics.DefaultRaycastLayers)
        {
            Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(ray, out RaycastHit hitInfo, maxDistance, layerMask))
            {
                worldPosition = hitInfo.point;
                return true;
            }

            worldPosition = ray.origin + ray.direction * maxDistance;
            return false;
        }
    }
}