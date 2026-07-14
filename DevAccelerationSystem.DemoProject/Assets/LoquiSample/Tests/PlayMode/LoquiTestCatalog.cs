using System.Collections.Generic;
using Loqui;
using UnityEngine;

namespace LoquiSample.Tests
{
    internal static class LoquiTestCatalog
    {
        public static LocalizationSettingsScope Settings(List<Object> sink)
        {
            var catalog = Create<LocalizationCatalog>(sink);
            catalog.Languages = new List<LocalizationLocaleProfile>
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

            catalog.Texts = new List<LocalizationEntry>
            {
                new()
                {
                    Key = "greeting",
                    EnglishFallback = "Hello",
                    Group = "demo",
                    Languages = new List<LocalizationLanguageValue>
                    {
                        new() { LanguageCode = "en", Values = new LocalizationPlatformValues { Default = "Hello" } },
                        new() { LanguageCode = "pt-BR", Values = new LocalizationPlatformValues { Default = "Olá" } }
                    }
                }
            };

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
