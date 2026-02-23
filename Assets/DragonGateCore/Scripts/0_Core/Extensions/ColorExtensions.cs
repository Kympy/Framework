using System.Runtime.CompilerServices;
using UnityEngine;

namespace DragonGate
{
    public static class ColorExtensions
    {
        // 유니티 컬러의 알파를 재지정
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color SetAlpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        // 알파 제거된 색상 반환 (채널은 존재)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color RemoveAlpha(this Color color)
        {
            color.a = 1f;
            return color;
        }

        /// <summary>
        /// 색상 반전, 투명도 유지
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color Reverse(this Color color)
        {
            return new Color(1f - color.r, 1f - color.g, 1f - color.b, color.a);
        }

        // Luma grayscale 변환
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color ToGrayscale(this Color color)
        {
            var gray = color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
            return new Color(gray, gray, gray, color.a);
        }
    }
}
