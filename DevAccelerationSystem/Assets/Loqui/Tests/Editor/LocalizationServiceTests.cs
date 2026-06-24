using System.Collections.Generic;
using Loqui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Loqui.Tests
{
    [TestFixture]
    public class LocalizationServiceTests
    {
        private readonly List<Object> _created = new();

        [SetUp]
        public void SetUp()
        {
            LocalizationPreferences.ClearExplicitChoice();
        }

        [TearDown]
        public void TearDown()
        {
            LocalizationPreferences.ClearExplicitChoice();
            foreach (var obj in _created)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }

            _created.Clear();
        }

        private LocalizationService Service(bool enabled, SystemLanguage systemLanguage = SystemLanguage.English)
        {
            var catalog = LocalizationTestAssets.Catalog(_created);
            var settings = LocalizationTestAssets.Settings(catalog, enabled);
            var service = new LocalizationService(settings, systemLanguage, LocalizationPlatform.Default);
            service.Initialize();
            return service;
        }

        [Test]
        public void ActiveLanguage_TakesPrecedenceOverEnglish()
        {
            var service = Service(true, SystemLanguage.Portuguese);

            Assert.AreEqual(LocalizationLanguageCodes.BrazilianPortuguese, service.CurrentLanguageCode);
            Assert.IsTrue(service.TryGet("greeting", out var value));
            Assert.AreEqual("Olá", value);
        }

        [Test]
        public void MissingActiveLanguageValue_FallsBackToEnglish()
        {
            var service = Service(true, SystemLanguage.Portuguese);

            Assert.IsTrue(service.TryGet("english_only", out var value));
            Assert.AreEqual("OnlyEnglish", value);
        }

        [Test]
        public void MissingKey_ReturnsProvidedFallback()
        {
            var service = Service(true);

            Assert.IsFalse(service.TryGet("does.not.exist", out _));
            Assert.AreEqual("fallback", service.Get("does.not.exist", "fallback"));
        }

        [Test]
        public void DisabledLocalization_ReturnsFallback()
        {
            var service = Service(false);

            Assert.IsFalse(service.IsEnabled);
            Assert.IsTrue(service.IsReady);
            Assert.IsFalse(service.TryGet("greeting", out _));
            Assert.AreEqual("design-time", service.Get("greeting", "design-time"));
        }

        [Test]
        public void SetLanguage_RefreshesActiveValues()
        {
            var service = Service(true);
            Assert.IsTrue(service.TryGet("greeting", out var english));
            Assert.AreEqual("Hello", english);

            var changed = false;
            service.LanguageChanged += () => changed = true;

            Assert.IsTrue(service.SetLanguage(LocalizationLanguageCodes.BrazilianPortuguese));
            Assert.IsTrue(changed);
            Assert.AreEqual(LocalizationLanguageCodes.BrazilianPortuguese, service.CurrentLanguageCode);
            Assert.IsTrue(service.TryGet("greeting", out var portuguese));
            Assert.AreEqual("Olá", portuguese);
        }

        [Test]
        public void SetLanguage_UnsupportedCode_ReturnsFalse()
        {
            var service = Service(true);

            Assert.IsFalse(service.SetLanguage("fr-FR"));
            Assert.AreEqual(LocalizationLanguageCodes.English, service.CurrentLanguageCode);
        }

        [Test]
        public void ResetToSystemLanguage_RestoresSystemChoice()
        {
            var service = Service(true, SystemLanguage.English);
            Assert.IsTrue(service.SetLanguage(LocalizationLanguageCodes.BrazilianPortuguese));

            Assert.IsTrue(service.ResetToSystemLanguage());
            Assert.AreEqual(LocalizationLanguageCodes.English, service.CurrentLanguageCode);
            Assert.IsFalse(LocalizationPreferences.HasExplicitChoice);
        }

        [Test]
        public void AvailableLanguages_ReflectEnabledLocales()
        {
            var service = Service(true);

            Assert.AreEqual(2, service.AvailableLanguages.Count);
        }

        [Test]
        public void WarmTryGet_DoesNotAllocate()
        {
            var service = Service(true);
            TestDelegate probe = () => service.TryGet("greeting", out _);
            for (var i = 0; i < 4; i++)
            {
                probe();
            }

            Assert.That(probe, Is.Not.AllocatingGCMemory());
        }
    }
}
