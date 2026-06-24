using System.Collections.Generic;
using System.Globalization;

namespace Loqui
{
    public static class LocalizationValidator
    {
        public static LocalizationValidationReport Validate(LocalizationSettingsScope settings)
        {
            var report = new LocalizationValidationReport();
            if (settings == null)
            {
                report.Error("Localization settings scope is null.");
                return report;
            }

            var catalog = settings.Catalog;
            if (catalog == null)
            {
                if (settings.EnabledByDefault)
                {
                    report.Warning("Localization is enabled but no catalog is assigned; UI will use component English fallbacks.");
                }

                return report;
            }

            if (!catalog.IsValid(out var error))
            {
                report.Error($"Catalog is invalid: {error}");
                return report;
            }

            var defaultCode = string.IsNullOrEmpty(settings.DefaultLanguageCode)
                ? LocalizationLanguageCodes.English
                : settings.DefaultLanguageCode;

            if (!catalog.TryGetLocale(defaultCode, out var defaultLocale))
            {
                report.Error($"Default language '{defaultCode}' is not present in the catalog.");
            }
            else if (!defaultLocale.Enabled)
            {
                report.Warning($"Default language '{defaultCode}' is present but disabled.");
            }

            var enabledCodes = new List<string>();
            catalog.CollectEnabledLanguageCodes(enabledCodes);
            if (enabledCodes.Count == 0)
            {
                report.Error("Catalog has no enabled languages.");
            }

            foreach (var code in enabledCodes)
            {
                if (!catalog.TryGetFontProfile(code, out var fontProfile) || fontProfile == null || !fontProfile.HasTmpFont)
                {
                    report.Warning($"Language '{code}' has no TMP font profile; labels will keep their existing component font.");
                }

                if (catalog.TryGetLocale(code, out var locale) && !string.IsNullOrEmpty(locale.CultureName) && !IsKnownCulture(locale.CultureName))
                {
                    report.Warning($"Language '{code}' uses culture name '{locale.CultureName}' which is not recognized; locale formatting will fall back to invariant.");
                }
            }

            return report;
        }

        private static bool IsKnownCulture(string cultureName)
        {
            try
            {
                CultureInfo.GetCultureInfo(cultureName);
                return true;
            }
            catch (CultureNotFoundException)
            {
                return false;
            }
        }
    }
}
