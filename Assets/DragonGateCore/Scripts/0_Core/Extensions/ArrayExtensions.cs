namespace DragonGate
{
    public static class ArrayExtensions
    {
        // 랜덤한 요소의 인덱스를 반환
        public static int GetRandomIndex<T>(this T[] array)
        {
            int length = array.Length;
            if (length == 1) return 0;
            int randomIndex = UnityEngine.Random.Range(0, length);
            return randomIndex;
        }

        // 랜덤한 요소를 반환
        public static T GetRandomElement<T>(this T[] array)
        {
            if (array.Length == 1) return array[0];
            return array[GetRandomIndex(array)];
        }

        // 배열 섞기
        public static void Shuffle<T>(this T[] array)
        {
            for (int i = 0; i < array.Length - 1; i++)
            {
                int targetIndex = UnityEngine.Random.Range(i, array.Length);
                (array[i], array[targetIndex]) = (array[targetIndex], array[i]);
            }
        }
    }
}