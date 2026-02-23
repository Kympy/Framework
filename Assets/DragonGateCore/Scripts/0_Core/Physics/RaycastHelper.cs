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
    }
}
