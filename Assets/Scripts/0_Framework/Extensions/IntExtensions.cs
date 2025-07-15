namespace Framework.Extensions
{
    public static class IntExtensions
    {
        public static int Pow2(this int src)
        {
            return src * src;
        }

        public static int Pow3(this int src)
        {
            return src * src * src;
        }

        public static float Half(this int src)
        {
            return src * 0.5f;
        }
        
        public static float Quarter(this int src)
        {
            return src * 0.25f;
        }

        public static bool IsBetween(this int src, float start, float end)
        {
            return src >= start && src <= end;
        }
    }
}
