using UnityEngine;
using UnityEngine.UI;

namespace DragonGate
{
    public static class CanvasExtensions
    {
        public static void SetCanvasMatchWidthOrHeight(this CanvasScaler scaler)
        {
            float screenRatio = GetScreenRatio();
            float referenceRatio = scaler.referenceResolution.x / scaler.referenceResolution.y;
            float result = Mathf.InverseLerp(0.5f, 2.4f, screenRatio / referenceRatio);
            float clamped = Mathf.Clamp01(result);
            scaler.matchWidthOrHeight = clamped;
        }

        private static float GetScreenRatio()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                case RuntimePlatform.IPhonePlayer:
                {
                    return Screen.safeArea.width / Screen.safeArea.height;
                }
                default:
                {
                    return (float)Screen.width / Screen.height;
                }
            }
        }
    }
}