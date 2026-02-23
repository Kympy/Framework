using System;
using System.Collections.Generic;

namespace DragonGate
{
    // 최초 접근 시 캐싱된 enum 배열을 생성하고, 반복 접근 시 캐싱된 enum을 사용하도록 하여 플레이 중 GC 발생을 최소화
    public static class EnumHelper<T> where T : struct, Enum
    {
        private static readonly T[] Values;
        private static Dictionary<T, string> Names;

        static EnumHelper()
        {
            Values = (T[])Enum.GetValues(typeof(T));
            Names = new();
            foreach (var value in Values)
            {
                Names[value] = value.ToString();
            }
        }
        
        // 랜덤한 enum 을 반환한다.
        public static T GetRandom() => Values[UnityEngine.Random.Range(0, Values.Length)];
        // 랜덤한 enum 인덱스를 반환
        public static int GetRandomIndex() => UnityEngine.Random.Range(0, Values.Length);
        // 캐싱된 이름을 반환
        public static string GetName(T value) => Names[value];
    }
    
    public static class EnumExtensions
    {
        
    }
}