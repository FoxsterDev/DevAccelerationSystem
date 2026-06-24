using System.Collections.Generic;
using UnityEngine;

namespace Loqui
{
    public static class LocalizationLanguageResolver
    {
        public static string MapSystemLanguage(SystemLanguage systemLanguage)
        {
            switch (systemLanguage)
            {
                case SystemLanguage.English:
                    return LocalizationLanguageCodes.English;
                case SystemLanguage.Portuguese:
                    return LocalizationLanguageCodes.BrazilianPortuguese;
                default:
                    return null;
            }
        }

        public static string Resolve(
            string persistedChoice,
            SystemLanguage systemLanguage,
            IReadOnlyList<string> supportedCodes,
            string fallback = LocalizationLanguageCodes.English)
        {
            if (TryMatchSupported(persistedChoice, supportedCodes, out var explicitChoice))
            {
                return explicitChoice;
            }

            var mapped = MapSystemLanguage(systemLanguage);
            if (TryMatchSupported(mapped, supportedCodes, out var mappedChoice))
            {
                return mappedChoice;
            }

            return fallback;
        }

        private static bool TryMatchSupported(string code, IReadOnlyList<string> supportedCodes, out string matched)
        {
            matched = null;
            if (string.IsNullOrEmpty(code) || supportedCodes == null)
            {
                return false;
            }

            for (var i = 0; i < supportedCodes.Count; i++)
            {
                if (LocalizationLanguageCodes.Equals(supportedCodes[i], code))
                {
                    matched = supportedCodes[i];
                    return true;
                }
            }

            return false;
        }
    }
}
