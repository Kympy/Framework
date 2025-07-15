using UnityEngine;

namespace Framework.Extensions
{
    /// <summary>
    /// 편의성을 위한 string 확장 기능
    /// </summary>
    public static class StringExtensions
    {
        private const string prefix = "<color=#";
        private const string prefixCloser = ">";
        private const string suffix = "</color>";
        public static string AddColor(this string origin, Color color)
        {
            return $"{prefix}{ColorUtility.ToHtmlStringRGB(color)}{prefixCloser}{origin}{suffix}";
        }
        
        public static bool IsNull(this string src)
        {
            return src == null;
        }

        private const string emptyString = "";
        public static bool IsEmpty(this string src)
        {
            return src == emptyString;
        }

        public static bool IsNullOrEmpty(this string src)
        {
            return IsNull(src) || IsEmpty(src);
        }

        public static Color ToColor(this string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color result))
            {
                return result;
            }

            DGLog.Log($"Failed to parsing hex string to color. {hex}", Color.red);
            return Color.white;
        }
        
        public static string SetBold(this string str)
        {
            return $"<b>{str}</b>";
        }

        public static string SetItalic(this string str)
        {
            return $"<i>{str}</i>";
        }
    }
}
