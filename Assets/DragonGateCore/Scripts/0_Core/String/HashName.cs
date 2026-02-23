using System.Collections.Generic;

namespace DragonGate
{
    public static class HashName
    {
        private const ulong OffsetBasis = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        private static readonly Dictionary<string, long> _cache = new();

        public static long ToHash(this string source)
        {
            return ToLongHash(source);
        }
        
        public static long ToLongHash(string source)
        {
            if (_cache.TryGetValue(source, out long cachedHash))
                return cachedHash;

            unchecked
            {
                ulong hash = OffsetBasis;

                for (int i = 0; i < source.Length; i++)
                {
                    hash ^= source[i];
                    hash *= Prime;
                }

                long result = (long)hash;
                _cache[source] = result;
                return result;
            }
        }
    }
}
