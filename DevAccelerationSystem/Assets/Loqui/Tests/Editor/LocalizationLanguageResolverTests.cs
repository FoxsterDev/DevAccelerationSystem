using System.Collections.Generic;
using Loqui;
using NUnit.Framework;
using UnityEngine;

namespace Loqui.Tests
{
    [TestFixture]
    public class LocalizationLanguageResolverTests
    {
        private static readonly IReadOnlyList<string> Supported = new List<string>
        {
            LocalizationLanguageCodes.English,
            LocalizationLanguageCodes.BrazilianPortuguese
        };

        [Test]
        public void ExplicitChoice_TakesPriority_OverSystemLanguage()
        {
            var resolved = LocalizationLanguageResolver.Resolve(
                LocalizationLanguageCodes.BrazilianPortuguese,
                SystemLanguage.English,
                Supported);

            Assert.AreEqual(LocalizationLanguageCodes.BrazilianPortuguese, resolved);
        }

        [Test]
        public void SystemLanguage_Portuguese_MapsToBrazilianPortuguese()
        {
            var resolved = LocalizationLanguageResolver.Resolve(
                string.Empty,
                SystemLanguage.Portuguese,
                Supported);

            Assert.AreEqual(LocalizationLanguageCodes.BrazilianPortuguese, resolved);
        }

        [Test]
        public void SystemLanguage_English_MapsToEnglish()
        {
            var resolved = LocalizationLanguageResolver.Resolve(
                string.Empty,
                SystemLanguage.English,
                Supported);

            Assert.AreEqual(LocalizationLanguageCodes.English, resolved);
        }

        [Test]
        public void UnsupportedSystemLanguage_FallsBackToEnglish()
        {
            var resolved = LocalizationLanguageResolver.Resolve(
                string.Empty,
                SystemLanguage.German,
                Supported);

            Assert.AreEqual(LocalizationLanguageCodes.English, resolved);
        }

        [Test]
        public void UnsupportedExplicitChoice_IsIgnored_AndSystemLanguageUsed()
        {
            var resolved = LocalizationLanguageResolver.Resolve(
                "de-DE",
                SystemLanguage.Portuguese,
                Supported);

            Assert.AreEqual(LocalizationLanguageCodes.BrazilianPortuguese, resolved);
        }

        [Test]
        public void NullSupportedList_FallsBackToEnglish()
        {
            var resolved = LocalizationLanguageResolver.Resolve(
                LocalizationLanguageCodes.BrazilianPortuguese,
                SystemLanguage.Portuguese,
                null);

            Assert.AreEqual(LocalizationLanguageCodes.English, resolved);
        }
    }
}
