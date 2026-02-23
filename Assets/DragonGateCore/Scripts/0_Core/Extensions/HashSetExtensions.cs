using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    public static class HashSetExtensions
    {
        public static T GetRandom<T>(this HashSet<T> set)
        {
            int count = set.Count;
            int targetIndex = Random.Range(0, count);
            int currentIndex = 0;
            foreach (var element in set)
            {
                if (currentIndex == targetIndex)
                    return element;
                currentIndex++;
            }
            return default;
        }
    }
}