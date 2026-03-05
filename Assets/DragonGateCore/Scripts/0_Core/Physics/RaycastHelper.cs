using UnityEngine;
using UnityEngine.InputSystem;

namespace DragonGate
{
    public class RaycastHelper
    {
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
