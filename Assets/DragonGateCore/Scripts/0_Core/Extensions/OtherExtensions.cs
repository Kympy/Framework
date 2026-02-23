using UnityEngine;

namespace DragonGate
{
    public static class OtherExtensions
    {
        // 이분법이 가능한 선택지에 대해 50:50 의 bool 값을 반환 (System.Random)
        public static bool Bool(this System.Random random)
        {
            int randomInt = random.Next(0, 2);
            return randomInt == 0;
        }
    }
}