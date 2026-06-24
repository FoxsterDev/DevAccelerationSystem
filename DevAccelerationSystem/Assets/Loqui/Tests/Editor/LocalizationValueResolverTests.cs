using System.Collections.Generic;
using Loqui;
using NUnit.Framework;

namespace Loqui.Tests
{
    [TestFixture]
    public class LocalizationValueResolverTests
    {
        private static LocalizationEntry BuildEntry()
        {
            return new LocalizationEntry
            {
                Key = "shop.title",
                EnglishFallback = "Buy",
                Languages = new List<LocalizationLanguageValue>
                {
                    new()
                    {
                        LanguageCode = LocalizationLanguageCodes.English,
                        Values = new LocalizationPlatformValues { Default = "Buy", IOS = "Withdraw" }
                    },
                    new()
                    {
                        LanguageCode = LocalizationLanguageCodes.BrazilianPortuguese,
                        Values = new LocalizationPlatformValues { Default = "Comprar", Android = "Adquirir" }
                    }
                }
            };
        }

        [Test]
        public void ActiveLanguage_PlatformSpecific_WinsOverDefault()
        {
            var entry = BuildEntry();
            var ok = LocalizationValueResolver.TryResolve(entry, LocalizationLanguageCodes.BrazilianPortuguese, LocalizationPlatform.Android, out var value);

            Assert.IsTrue(ok);
            Assert.AreEqual("Adquirir", value);
        }

        [Test]
        public void ActiveLanguage_FallsBackToDefault_WhenPlatformMissing()
        {
            var entry = BuildEntry();
            var ok = LocalizationValueResolver.TryResolve(entry, LocalizationLanguageCodes.BrazilianPortuguese, LocalizationPlatform.IOS, out var value);

            Assert.IsTrue(ok);
            Assert.AreEqual("Comprar", value);
        }

        [Test]
        public void MissingActiveLanguage_FallsBackToEnglish_PlatformSpecific()
        {
            var entry = BuildEntry();
            var ok = LocalizationValueResolver.TryResolve(entry, "fr-FR", LocalizationPlatform.IOS, out var value);

            Assert.IsTrue(ok);
            Assert.AreEqual("Withdraw", value);
        }

        [Test]
        public void MissingActiveLanguage_FallsBackToEnglishDefault()
        {
            var entry = BuildEntry();
            var ok = LocalizationValueResolver.TryResolve(entry, "fr-FR", LocalizationPlatform.Android, out var value);

            Assert.IsTrue(ok);
            Assert.AreEqual("Buy", value);
        }

        [Test]
        public void NoLanguageValues_FallsBackToEnglishFallbackString()
        {
            var entry = new LocalizationEntry
            {
                Key = "empty",
                EnglishFallback = "Hard Fallback",
                Languages = new List<LocalizationLanguageValue>()
            };

            var ok = LocalizationValueResolver.TryResolve(entry, LocalizationLanguageCodes.BrazilianPortuguese, LocalizationPlatform.Default, out var value);

            Assert.IsTrue(ok);
            Assert.AreEqual("Hard Fallback", value);
        }

        [Test]
        public void NullEntry_ReturnsFalse()
        {
            var ok = LocalizationValueResolver.TryResolve(null, LocalizationLanguageCodes.English, LocalizationPlatform.Default, out var value);

            Assert.IsFalse(ok);
            Assert.IsNull(value);
        }
    }
}
