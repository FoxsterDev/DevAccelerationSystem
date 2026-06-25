using System;
using System.Collections.Generic;
using UnityEngine;

namespace Loqui
{
    [CreateAssetMenu(fileName = "LocalizationCatalog", menuName = "Loqui/Catalog")]
    public sealed class LocalizationCatalog : ScriptableObject
    {
        public int SchemaVersion = 2;

        [Tooltip("Every shipped language. English is the required hard fallback and must be present.")]
        public List<LocalizationLocaleProfile> Languages = new();

        [Tooltip("All localized text entries. Keys are unique across the whole catalog; group them with the per-entry Group.")]
        public List<LocalizationEntry> Texts = new();

        [Tooltip("Non-text per-platform bool config (e.g. feature flags) resolved for the active platform at init.")]
        public List<LocalizationBoolEntry> Bools = new();

        public bool IsValid(out string error)
        {
            if (!ValidateLanguages(out error))
            {
                return false;
            }

            if (!ValidateTexts(out error))
            {
                return false;
            }

            return ValidateBools(out error);
        }

        bool ValidateLanguages(out string error)
        {
            if (Languages == null || Languages.Count == 0)
            {
                error = "Catalog has no languages.";
                return false;
            }

            var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var hasEnglish = false;
            foreach (var locale in Languages)
            {
                if (locale == null || string.IsNullOrEmpty(locale.LanguageCode))
                {
                    error = "Catalog has a language with no language code.";
                    return false;
                }

                if (!seenCodes.Add(locale.LanguageCode))
                {
                    error = $"Catalog has a duplicate language code '{locale.LanguageCode}'.";
                    return false;
                }

                if (LocalizationLanguageCodes.IsEnglish(locale.LanguageCode))
                {
                    hasEnglish = true;
                }
            }

            if (!hasEnglish)
            {
                error = "Catalog is missing the required English hard-fallback language.";
                return false;
            }

            error = null;
            return true;
        }

        bool ValidateTexts(out string error)
        {
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);
            if (Texts != null)
            {
                foreach (var entry in Texts)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(entry.Key))
                    {
                        error = "Catalog has a text entry with no key.";
                        return false;
                    }

                    if (!seenKeys.Add(entry.Key))
                    {
                        error = $"Catalog has a duplicate text key '{entry.Key}'.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        bool ValidateBools(out string error)
        {
            var seenBoolKeys = new HashSet<string>(StringComparer.Ordinal);
            if (Bools != null)
            {
                foreach (var entry in Bools)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(entry.Key))
                    {
                        error = "Catalog has a bool config entry with an empty key.";
                        return false;
                    }

                    if (!seenBoolKeys.Add(entry.Key))
                    {
                        error = $"Catalog has a duplicate bool config key '{entry.Key}'.";
                        return false;
                    }
                }
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

        public bool TryGetEntry(string key, out LocalizationEntry entry)
        {
            if (Texts != null && !string.IsNullOrEmpty(key))
            {
                foreach (var candidate in Texts)
                {
                    if (candidate != null && string.Equals(candidate.Key, key, StringComparison.Ordinal))
                    {
                        entry = candidate;
                        return true;
                    }
                }
            }

            entry = null;
            return false;
        }

        public void CollectEntries(List<LocalizationEntry> buffer)
        {
            if (buffer == null || Texts == null)
            {
                return;
            }

            foreach (var entry in Texts)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.Key))
                {
                    buffer.Add(entry);
                }
            }
        }

        public void CollectBoolEntries(List<LocalizationBoolEntry> buffer)
        {
            if (buffer == null || Bools == null)
            {
                return;
            }

            foreach (var entry in Bools)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.Key))
                {
                    buffer.Add(entry);
                }
            }
        }
    }
}
