using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace DragonGate
{
    public class DragonGateSystemLanguageSelector : IStartupLocaleSelector
    {
        public Locale GetStartupLocale(ILocalesProvider availableLocales)
        {
            // macOS에서도 systemLanguage가 더 정확함
            SystemLanguage systemLang = Application.systemLanguage;
            DGDebug.Log($"[Start Up Locale Selector] System Language: {systemLang}");
        
            string localeCode = systemLang switch
            {
                SystemLanguage.Korean => "ko",
                SystemLanguage.English => "en",
                SystemLanguage.Japanese => "ja",
                _ => "en"
            };
        
            Locale locale = availableLocales.GetLocale(localeCode);
        
            if (locale == null)
            {
                Debug.LogWarning($"Locale '{localeCode}' not found, trying alternatives...");
                // ko-KR 시도
                locale = availableLocales.GetLocale(localeCode + "-KR");
            }
        
            Debug.Log($"Selected Locale: {locale?.Identifier.Code ?? "fallback to en"}");
            return locale ?? availableLocales.GetLocale("en");
        }
    }
}
