using UnityEngine;
using UnityEngine.UI;

namespace DragonGate
{
    public static class ImageExtensions
    {
        public static void SetAlpha(this Image image, float alpha)
        {
            Color originColor = image.color;
            image.color = new Color(originColor.r, originColor.g, originColor.b, alpha);
        }
        
        // 기존 알파를 유지한 채 RGB만 변경
        public static void SetColorWithoutAlpha(this Image image, Color color)
        {
            var originAlpha = image.color.a;
            image.color = new Color(color.r, color.g, color.b, originAlpha);
        }

        // Color와 Alpha를 동시에 세팅
        public static void SetColorWithAlpha(this Image image, Color color, float alpha)
        {
            image.color = new Color(color.r, color.g, color.b, alpha);
        }

        public static void SetVisible(this Image image, bool visible)
        {
            image.SetAlpha(visible ? 1 : 0);
        }
    }
}
