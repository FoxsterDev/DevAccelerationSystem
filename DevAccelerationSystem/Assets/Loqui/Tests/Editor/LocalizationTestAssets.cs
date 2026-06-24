using System.Collections.Generic;
using Loqui;
using UnityEngine;

namespace Loqui.Tests
{
    internal static class LocalizationTestAssets
    {
        public static LocalizationCatalog Catalog(List<Object> sink, bool withPortuguese = true)
        {
            var locales = Create<LocalizationLocaleSet>(sink);
            locales.Languages = new List<LocalizationLocaleProfile>
            {
                new()
                {
                    LanguageCode = LocalizationLanguageCodes.English,
                    DisplayName = "English",
                    NativeDisplayName = "English",
                    CultureName = "en",
                    Enabled = true
                }
            };

            if (withPortuguese)
            {
                locales.Languages.Add(new LocalizationLocaleProfile
                {
                    LanguageCode = LocalizationLanguageCodes.BrazilianPortuguese,
                    DisplayName = "Portuguese (Brazil)",
                    NativeDisplayName = "Português (Brasil)",
                    CultureName = "pt-BR",
                    Enabled = true
                });
            }

            var table = Create<LocalizationTextTable>(sink);
            table.Group = "test";
            table.Entries = new List<LocalizationEntry>
            {
                Entry("greeting", "Hello", ("en", "Hello"), ("pt-BR", "Olá")),
                Entry("english_only", "OnlyEnglish", ("en", "OnlyEnglish"))
            };

            var catalog = Create<LocalizationCatalog>(sink);
            catalog.Locales = locales;
            catalog.TextTables = new List<LocalizationTextTable> { table };
            return catalog;
        }

        public static LocalizationSettingsScope Settings(LocalizationCatalog catalog, bool enabled)
        {
            return new LocalizationSettingsScope
            {
                EnabledByDefault = enabled,
                Catalog = catalog,
                DefaultLanguageCode = LocalizationLanguageCodes.English
            };
        }

        private static LocalizationEntry Entry(string key, string englishFallback, params (string code, string value)[] languages)
        {
            var entry = new LocalizationEntry
            {
                Key = key,
                EnglishFallback = englishFallback,
                Languages = new List<LocalizationLanguageValue>()
            };

            foreach (var (code, value) in languages)
            {
                entry.Languages.Add(new LocalizationLanguageValue
                {
                    LanguageCode = code,
                    Values = new LocalizationPlatformValues { Default = value }
                });
            }

            return entry;
        }

        private static T Create<T>(List<Object> sink) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            sink.Add(asset);
            return asset;
        }
    }
}
