using System.Collections.Generic;
using UnityEngine;

namespace Framework.Extensions
{
    /// <summary>
    /// 리스트 확장 메서드
    /// 만들고 보니 이미 자체적으로 존재하는 기능들도 있지만..확장메서드가 사용이 편리하므로 이걸 사용하겠음..
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// 제거하는 인덱스와 맨 마지막 인덱스를 스왑 후 제거 O(1) 에 끝남.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="index"></param>
        /// <typeparam name="T"></typeparam>
        public static void RemoveAtSwapBack<T>(this List<T> list, int index)
        {
            T temp = list[index];
            list[index] = list[^1];
            list[^1] = temp;
            
            list.RemoveAt(list.Count - 1);
        }
        
        /// <summary>
        /// 제거하는 요소와 마지막 요소를 스왑 후 제거. 인덱스 검색 비용이 있어서 O(n) 으로 기존과 동일하지만
        /// 배열 재할당이 없어지므로 이득이 여전히 있음.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="target"></param>
        /// <typeparam name="T"></typeparam>
        public static void RemoveSwapBack<T>(this List<T> list, T target)
        {
            int targetIndex = list.IndexOf(target);
            T temp = list[targetIndex];
            list[targetIndex] = list[^1];
            list[^1] = temp;

            list.RemoveAt(list.Count - 1);
        }

        public static void Shuffle<T>(this List<T> list)
        {
            int length = list.Count;
            for (int i = 0; i < length - 1; i++)
            {
                int randIndex = Random.Range(i, length);
                if (randIndex == i) continue;

                T temp = list[i];
                list[i] = list[randIndex];
                list[randIndex] = temp;
            }
        }

        public static T GetRandom<T>(this List<T> list)
        {
            int index = Random.Range(0, list.Count);
            return list[index];
        }
    }
}
