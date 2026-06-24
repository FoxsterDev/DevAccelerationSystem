using System;
using System.Collections.Generic;
using UnityEngine;

namespace Loqui
{
    [CreateAssetMenu(fileName = "LocalizationLocaleSet", menuName = "Loqui/Locale Set")]
    public sealed class LocalizationLocaleSet : ScriptableObject
    {
        public List<LocalizationLocaleProfile> Languages = new();

        public bool Validate(out string error)
        {
            if (Languages == null || Languages.Count == 0)
            {
                error = "Locale set has no languages.";
                return false;
            }

            var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var hasEnglish = false;
            foreach (var locale in Languages)
            {
                if (locale == null || string.IsNullOrEmpty(locale.LanguageCode))
                {
                    error = "Locale set has a language with no language code.";
                    return false;
                }

                if (!seenCodes.Add(locale.LanguageCode))
                {
                    error = $"Locale set has a duplicate language code '{locale.LanguageCode}'.";
                    return false;
                }

                if (LocalizationLanguageCodes.IsEnglish(locale.LanguageCode))
                {
                    hasEnglish = true;
                }
            }

            if (!hasEnglish)
            {
                error = "Locale set is missing the required English hard-fallback language.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryGetLocale(string languageCode, out LocalizationLocaleProfile locale)
        {
            if (Languages != null && !string.IsNullOrEmpty(languageCode))
            {
                foreach (var candidate in Languages)
                {
                    if (candidate != null && LocalizationLanguageCodes.Equals(candidate.LanguageCode, languageCode))
                    {
                        locale = candidate;
                        return true;
                    }
                }
            }

            locale = null;
            return false;
        }

        public bool TryGetFontProfile(string languageCode, out LocalizationFontProfile fontProfile)
        {
            if (TryGetLocale(languageCode, out var locale) && locale.FontProfile != null)
            {
                fontProfile = locale.FontProfile;
                return true;
            }

            fontProfile = null;
            return false;
        }

        public void CollectEnabledLanguageCodes(List<string> buffer)
        {
            if (buffer == null || Languages == null)
            {
                return;
            }

            foreach (var locale in Languages)
            {
                if (locale != null && locale.Enabled && !string.IsNullOrEmpty(locale.LanguageCode))
                {
                    buffer.Add(locale.LanguageCode);
                }
            }
        }
    }
}
