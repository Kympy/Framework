using UnityEngine;
using UnityEngine.EventSystems;

namespace DragonGate
{
    public struct HSVColor
    {
        public float H;
        public float S;
        public float V;

        public HSVColor(float h, float s, float v)
        {
            H = h;
            S = s;
            V = v;
        }
    }
    
    public struct HueSaturation
    {
        public float Hue;
        public float Saturation;
    }

    public static class HSVColorExtensions
    {
        // 기존 유니티 컬러를 hsv로 변경
        public static HSVColor RGBToHSV(this Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            return new HSVColor(h, s, v);
        }

        // hsv 를 유니티 컬러로 변경
        public static Color HSVToRGB(this HSVColor color)
        {
            return Color.HSVToRGB(color.H, color.S, color.V);
        }
        
        // Hsv 컬러 휠 텍스쳐를 생성한다. 반지름은 정수 픽셀 단위로 입력 필요
        public static Texture2D CreateColorWheel(int textureRadius)
        {
            int width = textureRadius * 2;
            // Use a concrete format and no mipmaps for UI use
            Texture2D texture = new Texture2D(width, width, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            // Center in texture pixel space
            Vector2 center = new Vector2(textureRadius, textureRadius);

            for (int y = 0; y < width; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // sample at pixel center to avoid seams
                    Vector2 pos = new Vector2(x + 0.5f, y + 0.5f);
                    Vector2 dir = pos - center;
                    float dist = dir.magnitude / textureRadius; // 0..1

                    if (dist > 1f)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    // Hue must be in 0..1 for HSVToRGB
                    float hue01 = (Mathf.Atan2(dir.y, dir.x) + Mathf.PI) / (Mathf.PI * 2f); // 0..1
                    float sat = Mathf.Clamp01(dist);
                    Color col = Color.HSVToRGB(hue01, sat, 1f);
                    texture.SetPixel(x, y, col);
                }
            }

            texture.Apply();
            return texture;
        }

        // rect 상에서 포인터 위치의 HS 값을 가져온다.
        public static HueSaturation? GetHueSaturation(PointerEventData eventData, RectTransform rect)
        {
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out localPos);
            return GetHueSaturation(localPos, rect);
        }

        public static HueSaturation? GetHueSaturation(Vector2 localPosition, RectTransform rectTransform)
        {
            Vector2 center = new Vector2(rectTransform.rect.width * (0.5f - rectTransform.pivot.x),
                                         rectTransform.rect.height * (0.5f - rectTransform.pivot.y));
            Vector2 dir = localPosition - center;
            float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
            float distance = dir.magnitude / radius;
            if (distance > 1f) return null;
            distance = Mathf.Clamp01(distance);
            
            var hs = new HueSaturation();
            hs.Hue = (Mathf.Atan2(dir.y, dir.x) + Mathf.PI) / (2 * Mathf.PI);
            hs.Saturation = distance;
            return hs;
        }
    }
}