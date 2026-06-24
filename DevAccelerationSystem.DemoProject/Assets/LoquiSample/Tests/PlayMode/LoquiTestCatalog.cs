using System.Collections.Generic;
using Loqui;
using UnityEngine;

namespace LoquiSample.Tests
{
    internal static class LoquiTestCatalog
    {
        public static LocalizationSettingsScope Settings(List<Object> sink)
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
                },
                new()
                {
                    LanguageCode = LocalizationLanguageCodes.BrazilianPortuguese,
                    DisplayName = "Portuguese (Brazil)",
                    NativeDisplayName = "Português (Brasil)",
                    CultureName = "pt-BR",
                    Enabled = true
                }
            };

            var table = Create<LocalizationTextTable>(sink);
            table.Group = "demo";
            table.Entries = new List<LocalizationEntry>
            {
                new()
                {
                    Key = "greeting",
                    EnglishFallback = "Hello",
                    Languages = new List<LocalizationLanguageValue>
                    {
                        new() { LanguageCode = "en", Values = new LocalizationPlatformValues { Default = "Hello" } },
                        new() { LanguageCode = "pt-BR", Values = new LocalizationPlatformValues { Default = "Olá" } }
                    }
                }
            };

            var catalog = Create<LocalizationCatalog>(sink);
            catalog.Locales = locales;
            catalog.TextTables = new List<LocalizationTextTable> { table };

            return new LocalizationSettingsScope
            {
                EnabledByDefault = true,
                Catalog = catalog,
                DefaultLanguageCode = LocalizationLanguageCodes.English
            };
        }

        private static T Create<T>(List<Object> sink) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            sink.Add(asset);
            return asset;
        }
    }
}
