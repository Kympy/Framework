using UnityEngine;

namespace DragonGate
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
        
        public static float Pow4(this float src)
        {
            return src * src * src * src;
        }

        public static float Twice(this float src)
        {
            return src * 2f;
        }

        public static float ThreeTimes(this float src)
        {
            return src * 3f;
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
            return IsSame(src, 0f);
        }

        // unity mathf.approximately 를 복사해 와 코드 가독성 용으로 사용하는 확장 함수
        public static bool IsSame(this float src, float other)
        {
            return (double)Mathf.Abs(src - other) <
                   (double)Mathf.Max(1E-06f * Mathf.Max(Mathf.Abs(src), Mathf.Abs(other)), Mathf.Epsilon * 8f);
        }

        public static bool IsLessThan(this float src, float other)
        {
            return src < other;
        }

        public static bool IsGreaterThan(this float src, float other)
        {
            return src > other;
        }

        /// <summary>
        /// N 번째 자리까지 표현된 소수
        /// </summary>
        /// <param name="src"></param>
        /// <param name="decimalPointCount">소수점의 갯수</param>
        /// <returns></returns>
        public static float Round(this float src, int decimalPointCount)
        {
            float multiplier = Mathf.Pow(10, decimalPointCount);
            return Mathf.Round(src * multiplier) / multiplier;
        }

        public static double Round(this double src, int decimalPointCount)
        {
            double multiplier = System.Math.Pow(10, decimalPointCount);
            return System.Math.Round(src * multiplier) / multiplier;
        }

        public static float Clamp01(this float src)
        {
            if (src < 0f) return 0f;
            if (src > 1f) return 1f;
            return src;
        }
    }
}
