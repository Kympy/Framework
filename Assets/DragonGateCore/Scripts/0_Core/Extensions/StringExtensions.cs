using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace DragonGate
{
    /// <summary>
    /// 편의성을 위한 string 확장 기능
    /// </summary>
    public static class StringExtensions
    {
        private const string prefix = "<color=#";
        private const string prefixCloser = ">";
        private const string suffix = "</color>";
        
        // 텍스트에 색상을 주입
        public static string SetColor(this string origin, Color color)
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

        // hex string 을 ColorUtility를 거치지 않고 빠르게 변환
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color ToColor(this string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color result))
            {
                return result;
            }
            DGDebug.Log($"Failed to parsing hex string to color. {hex}", Color.red);
            return Color.white;
        }
        
        // 볼드체
        public static string SetBold(this string str)
        {
            return $"<b>{str}</b>";
        }

        // 이탈릭체
        public static string SetItalic(this string str)
        {
            return $"<i>{str}</i>";
        }
        
        public static string RemoveWhitespaceAndSpecialCharacters(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var stringBuilder = new StringBuilder(input.Length);

            foreach (char character in input)
            {
                if (char.IsLetterOrDigit(character))
                    stringBuilder.Append(character);
            }

            return stringBuilder.ToString();
        }
    }
}
