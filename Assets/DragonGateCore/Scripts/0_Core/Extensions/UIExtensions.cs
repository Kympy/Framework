using UnityEngine;
using UnityEngine.UI;

namespace DragonGate
{
    public static class UIExtensions
    {
        public static void SetAlpha(this Graphic graphic, float alpha)
        {
            var origin = graphic.color;
            graphic.color = new Color(origin.r, origin.g, origin.b, alpha);
        }
    }
}