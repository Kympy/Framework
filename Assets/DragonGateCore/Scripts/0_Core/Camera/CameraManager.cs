using UnityEngine;

namespace DragonGate
{
    public class CameraManager
    {
        public static Camera CurrentCamera { get; private set; }

        public static void EnableCamera(Camera camera)
        {
            if (CurrentCamera != null)
            {
                CurrentCamera.gameObject.SetActive(false);
            }
            CurrentCamera = camera;
            CurrentCamera.gameObject.SetActive(true);
        }

        public static void DisableCamera(Camera camera = null)
        {
            if (camera == null)
            {
                if (CurrentCamera != null)
                    CurrentCamera.gameObject.SetActive(false);
                return;
            }
            camera.gameObject.SetActive(false);
        }
        
        public static bool TryGetScreenToWorldPosition(Vector2 screenPosition, out Vector3 worldPosition, float maxDistance = 1000f, int layerMask = ~0)
        {
            var ray = CurrentCamera.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out var hitInfo, maxDistance, layerMask))
            {
                worldPosition = hitInfo.point;
                return true;
            }
            worldPosition = default;
            return false;
        }

        public static Vector2 WorldToScreenPoint(Vector3 worldPosition)
        {
            var screenPosition = CurrentCamera.WorldToScreenPoint(worldPosition);
            return new Vector2(screenPosition.x, screenPosition.y);
        }
    }
}
