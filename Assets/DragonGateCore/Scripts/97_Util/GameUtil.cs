using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DragonGate
{
    public static class GameUtil
    {
        public static T ToEnum<T>(int value) where T : unmanaged, Enum
        {
            return UnsafeUtility.As<int, T>(ref value);
        }
        
        public static int ToInt<T>(T enumValue) where T : unmanaged, Enum
        {
            return UnsafeUtility.As<T, int>(ref enumValue);
        }
        
        public static string GetProjectPath()
        {
            return System.IO.Path.GetDirectoryName(Application.dataPath);
        }
        
        public static T CreateMonoInstance<T>(bool dontDestroy = false) where T : MonoBehaviour
        {
            var obj = new GameObject(typeof(T).ToString()).AddComponent<T>();
            if (dontDestroy)
                UnityEngine.Object.DontDestroyOnLoad(obj);
            return obj;
        }

        public static void SetActiveAll(GameObject go, GameObject go1, bool active)
        {
            if (go == null) return;
            if (go.activeSelf == active) return;
            go.SetActive(active);
            if (go1 == null) return;
            if (go1.activeSelf == active) return;
            go1.SetActive(active);
        }

        public static void SetActiveAll(GameObject go, GameObject go1, GameObject go2, bool active)
        {
            if (go == null || go.activeSelf == active) return;
            go.SetActive(active);
            if (go1 == null || go1.activeSelf == active) return;
            go1.SetActive(active);
            if (go2 == null || go2.activeSelf == active) return;
            go2.SetActive(active);
        }
        
        public static void SetActiveAll(GameObject go, GameObject go1, GameObject go2, GameObject go3, bool active)
        {
            if (go == null || go.activeSelf == active) return;
            go.SetActive(active);
            if (go1 == null || go1.activeSelf == active) return;
            go1.SetActive(active);
            if (go2 == null || go2.activeSelf == active) return;
            go2.SetActive(active);
            if (go3 == null || go3.activeSelf == active) return;
            go3.SetActive(active);
        }

        public static void ResizeSpriteWidthByPPU(SpriteRenderer spr, float width)
        {
            float spriteRadius = spr.sprite.rect.width * 0.5f / spr.sprite.pixelsPerUnit;
            float scaleFactor = width / spriteRadius;
            spr.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        }

        public static float ResizeSpriteRadiusByPPU(SpriteRenderer spr, float radius, float addScale = 0)
        {
            float spriteRadius = spr.sprite.rect.width * 0.5f / spr.sprite.pixelsPerUnit;
            float scaleFactor = radius / spriteRadius;
            scaleFactor += addScale;
            spr.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
            return scaleFactor;
        }
        
        public static void FitSpriteToCamera(SpriteRenderer spriteRenderer, Camera cam, float margin = 0)
        {
            float screenHeight = cam.orthographicSize * 2f; // 카메라 세로 크기(월드 단위)
            float screenWidth = screenHeight * cam.aspect;  // 카메라 가로 크기(월드 단위)

            Vector2 spriteSize = spriteRenderer.sprite.bounds.size - new Vector3(margin, margin, 0); // 스프라이트 실제 크기(월드 단위)

            float scaleX = screenWidth / spriteSize.x;
            float scaleY = screenHeight / spriteSize.y;

            // 비율 깨지지 않게 동일한 스케일 적용
            spriteRenderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
        
        // Helper to get world size of a SpriteRenderer
        public static Vector2 GetSpriteWorldSize(SpriteRenderer sr)
        {
            if (sr == null || sr.sprite == null)
                return Vector2.zero;
            // sprite.bounds is in local space; multiply by lossyScale to get world size
            var size = sr.sprite.bounds.size;
            var scale = sr.transform.lossyScale;
            return new Vector2(Mathf.Abs(size.x * scale.x), Mathf.Abs(size.y * scale.y));
        }

        public static bool RandomBool()
        {
            return Random.Range(0, 2) == 0;
        }

        // 이분법 랜덤에 대해 좌,우 가중치를 주어 선별
        public static bool RandomBoolWeighted(float leftWeight, float rightWeight)
        {
            // 1) 유효성 검사
            if (float.IsNaN(leftWeight) || float.IsNaN(rightWeight) ||
                float.IsInfinity(leftWeight) || float.IsInfinity(rightWeight))
                throw new ArgumentException("Weights must be finite numbers.");

            if (leftWeight < 0f || rightWeight < 0f)
                throw new ArgumentOutOfRangeException("Weights must be >= 0.");

            float sum = leftWeight + rightWeight;

            // 2) 합이 0이면 동전던지기로 정의(균등)
            if (sum <= 0f)
                return Random.value < 0.5f;

            // 3) 임계값 비교(더 간단하고 의도가 명확)
            float threshold = leftWeight / sum;
            return Random.value < threshold; // true = left 선택, false = right 선택
        }

        public static int RandomInt(int min, int max)
        {
            return Random.Range(min, max + 1);
        }
        
        public static bool IsInPercent(float targetPercent)
        {
            float clampedPercent = Mathf.Clamp(targetPercent, 0f, 100f);
            return Random.value < clampedPercent * 0.01f;
        }

        public static Vector3 RandomVector3(float min, float max)
        {
            return new Vector3(Random.Range(min, max), Random.Range(min, max), Random.Range(min, max));
        }

        public static Vector2 RandomVector2(float min, float max)
        {
            return new Vector2(Random.Range(min, max), Random.Range(min, max));
        }
        
        public static bool IsWorldPositionOutsideCamera(Camera cam, Vector3 worldPos, float tolerance = 0f)
        {
            Vector3 viewportPos = cam.WorldToViewportPoint(worldPos);

            var min = tolerance;
            var max = 1 - tolerance;
            // z가 음수면 카메라 뒤쪽
            if (viewportPos.z < 0)
                return true;

            // 뷰포트 좌표가 0~1 범위를 벗어났는지
            return viewportPos.x < min || viewportPos.x > max ||
                   viewportPos.y < min || viewportPos.y > max;
        }

        public static Vector2 WorldToScreenPosition(Vector3 worldPosition, Camera camera)
        {
            var screenPosition = camera.WorldToScreenPoint(worldPosition);
            return new Vector2(screenPosition.x, screenPosition.y);
        }
    }
}
