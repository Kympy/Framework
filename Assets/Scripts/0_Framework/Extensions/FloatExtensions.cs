using UnityEngine;

namespace Framework.Extensions
{
    public static class FloatExtensions
    {
        /// <summary>
        /// 자주 사용하는 제곱, 세제곱 등은 유니티 함수보다 직접 곰셉이 빠르므로 확장 메서드로 구현.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static float Pow2(this float src)
        {
            return src * src;
        }

        public static float Pow3(this float src)
        {
            return src * src * src;
        }

        public static float Half(this float src)
        {
            return src * 0.5f;
        }

        public static float Quarter(this float src)
        {
            return src * 0.25f;
        }
        
        public static bool IsBetween(this float src, float start, float end)
        {
            return src >= start && src <= end;
        }

        public static bool IsZero(this float src)
        {
            return Mathf.Approximately(src, 0);
        }

        /// <summary>
        /// N 번째 자리까지 표현된 소수
        /// </summary>
        /// <param name="src"></param>
        /// <param name="decimalPointCount"></param>
        /// <returns></returns>
        public static float Round(this float src, int decimalPointCount)
        {
            float multiplier = Mathf.Pow(10, decimalPointCount);
            return Mathf.Round(src * multiplier) / multiplier;
        }
    }
}
