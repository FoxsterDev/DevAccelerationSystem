namespace Loqui
{
    public static class LocalizationValueResolver
    {
        public static bool TryResolve(LocalizationEntry entry, string activeLanguageCode, LocalizationPlatform platform, out string value)
        {
            value = null;
            if (entry == null)
            {
                return false;
            }

            if (TryResolveLanguage(entry, activeLanguageCode, platform, out value))
            {
                return true;
            }

            if (!LocalizationLanguageCodes.IsEnglish(activeLanguageCode) &&
                TryResolveLanguage(entry, LocalizationLanguageCodes.English, platform, out value))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(entry.EnglishFallback))
            {
                value = entry.EnglishFallback;
                return true;
            }

            return false;
        }

        private static bool TryResolveLanguage(LocalizationEntry entry, string languageCode, LocalizationPlatform platform, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(languageCode))
            {
                return false;
            }

            return entry.TryGetLanguageValues(languageCode, out var values) && values.TryGet(platform, out value);
        }
    }
}
