using System;
using TMPro;

namespace DragonGate
{
    public static class TMPExtensions
    {
        public static void SetEmpty(this TMP_Text textComponent)
        {
            textComponent.SetText(string.Empty);
        }
        
        public static void SetInt(this TMP_Text text, int value)
        {
            text.SetText("{0}", value);
        }

        public static void SetFloat(this TMP_Text text, float value)
        {
            text.SetText("{0}", value);
        }

        public static void SetLong(this TMP_Text text, long value)
        {
            text.SetText("{0}", value);
        }

        public static void SetFloat1(this TMP_Text text, float value)
        {
            text.SetText("{0:0.0}", value);
        }
        
        public static void SetFloat2(this TMP_Text text, float value)
        {
            text.SetText("{0:0.00}", value);
        }
        
        public static void SetFloat3(this TMP_Text text, float value)
        {
            text.SetText("{0:0.000}", value);
        }

        public static void SetCurrency(this TMP_Text textComponent, int amount)
        {
            textComponent.SetText("{0:#,###}", amount);
        }
    }
}