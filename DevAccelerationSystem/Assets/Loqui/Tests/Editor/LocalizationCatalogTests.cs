using System.Collections.Generic;
using Loqui;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Loqui.Tests
{
    [TestFixture]
    public class LocalizationCatalogTests
    {
        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }

            _created.Clear();
        }

        private LocalizationCatalog Catalog(params string[] languageCodes)
        {
            var catalog = New<LocalizationCatalog>();
            catalog.Languages = new List<LocalizationLocaleProfile>();
            foreach (var code in languageCodes)
            {
                catalog.Languages.Add(new LocalizationLocaleProfile { LanguageCode = code, DisplayName = code });
            }

            return catalog;
        }

        private T New<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _created.Add(asset);
            return asset;
        }

        private static LocalizationEntry Entry(string key, string group, string value)
        {
            return new LocalizationEntry { Key = key, Group = group, EnglishFallback = value };
        }

        [Test]
        public void ValidCatalog_PassesValidation()
        {
            var catalog = Catalog(LocalizationLanguageCodes.English, LocalizationLanguageCodes.BrazilianPortuguese);
            catalog.Texts = new List<LocalizationEntry>
            {
                Entry("options.title", "options", "Options"),
                Entry("shop.title", "shop", "Buy")
            };

            Assert.IsTrue(catalog.IsValid(out var error), error);
        }

        [Test]
        public void Catalog_WithoutLanguages_IsInvalid()
        {
            var catalog = New<LocalizationCatalog>();
            catalog.Languages = null;

            Assert.IsFalse(catalog.IsValid(out _));
        }

        [Test]
        public void Catalog_WithoutEnglish_IsInvalid()
        {
            var catalog = Catalog(LocalizationLanguageCodes.BrazilianPortuguese);

            Assert.IsFalse(catalog.IsValid(out _));
        }

        [Test]
        public void Catalog_WithDuplicateLanguageCode_IsInvalid()
        {
            var catalog = Catalog(LocalizationLanguageCodes.English, LocalizationLanguageCodes.English);

            Assert.IsFalse(catalog.IsValid(out _));
        }

        [Test]
        public void Catalog_WithDuplicateTextKey_IsInvalid()
        {
            var catalog = Catalog(LocalizationLanguageCodes.English);
            catalog.Texts = new List<LocalizationEntry>
            {
                Entry("dup", "a", "1"),
                Entry("dup", "b", "2")
            };

            Assert.IsFalse(catalog.IsValid(out _));
        }

        [Test]
        public void Catalog_WithEmptyTextKey_IsInvalid()
        {
            var catalog = Catalog(LocalizationLanguageCodes.English);
            catalog.Texts = new List<LocalizationEntry> { Entry("", "a", "1") };

            Assert.IsFalse(catalog.IsValid(out _));
        }

        [Test]
        public void Catalog_TryGetEntry_SearchesAllTexts()
        {
            var catalog = Catalog(LocalizationLanguageCodes.English);
            catalog.Texts = new List<LocalizationEntry>
            {
                Entry("a.key", "a", "A"),
                Entry("b.key", "b", "B")
            };

            Assert.IsTrue(catalog.TryGetEntry("b.key", out var entry));
            Assert.AreEqual("B", entry.EnglishFallback);
            Assert.IsFalse(catalog.TryGetEntry("missing.key", out _));
        }

        [Test]
        public void Catalog_WithNullEntry_IsTolerated()
        {
            var catalog = Catalog(LocalizationLanguageCodes.English);
            catalog.Texts = new List<LocalizationEntry>
            {
                null,
                Entry("a.key", "a", "A")
            };

            Assert.IsTrue(catalog.IsValid(out var error), error);
            Assert.IsTrue(catalog.TryGetEntry("a.key", out _));
        }

        [Test]
        public void FontProfileLookup_ReturnsLocaleFont()
        {
            var primary = New<TMP_FontAsset>();
            var catalog = New<LocalizationCatalog>();
            catalog.Languages = new List<LocalizationLocaleProfile>
            {
                new()
                {
                    LanguageCode = LocalizationLanguageCodes.English,
                    FontProfile = new LocalizationFontProfile { PrimaryFont = primary }
                }
            };

            Assert.IsTrue(catalog.TryGetFontProfile(LocalizationLanguageCodes.English, out var profile));
            Assert.AreSame(primary, profile.ResolveTmpFont(LocalizationPlatform.Default));
        }

        [Test]
        public void FontProfile_PlatformOverride_WinsForThatPlatform()
        {
            var primary = New<TMP_FontAsset>();
            var androidFont = New<TMP_FontAsset>();
            var profile = new LocalizationFontProfile
            {
                PrimaryFont = primary,
                AndroidFontOverride = androidFont
            };

            Assert.AreSame(androidFont, profile.ResolveTmpFont(LocalizationPlatform.Android));
            Assert.AreSame(primary, profile.ResolveTmpFont(LocalizationPlatform.IOS));
        }
    }
}
