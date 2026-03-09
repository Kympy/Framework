using System;
using TMPro;
using UnityEngine;

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

        public static void SetCurrency(this TMP_Text textComponent, long amount)
        {
            textComponent.SetText("{0:#,###}", amount);
        }

        public static void SetPlus(this TMP_Text textComponent, int amount)
        {
            textComponent.SetText("+{0:#,###}", amount);
        }

        public static void SetPlus(this TMP_Text textComponent, long amount)
        {
            textComponent.SetText("+{0:#,###}", amount);
        }

        public static void SetMinus(this TMP_Text textComponent, int amount)
        {
            textComponent.SetText("-{0:#,###}", amount);
        }

        public static void SetMinus(this TMP_Text textComponent, long amount)
        {
            textComponent.SetText("-{0:#,###}", amount);
        }

        public static void SetTime(this TMP_Text textComponent, int hours, int minutes)
        {
            textComponent.SetText("{0:00}:{1:00}", hours, minutes);
        }

        public static void SetSprite(this TMP_Text textComponent, int index)
        {
            textComponent.SetText("<sprite={0}>", index);
        }

        public static void SetSprite(this TMP_Text textComponent, string spriteName)
        {
            textComponent.SetText($"<sprite name={spriteName}>");
        }

        // 0 ~ 100으로 percent 받음. 물론 100넘을수도있음. 백분율로 받는다는 얘기임.
        public static void SetPercent(this TMP_Text textComponent, int percent)
        {
            textComponent.SetText("{0}%", percent);
        }

        public static void SetFraction(this TMP_Text textComponent, int current, int max)
        {
            textComponent.SetText("{0}/{1}", current, max);
        }
    }
}