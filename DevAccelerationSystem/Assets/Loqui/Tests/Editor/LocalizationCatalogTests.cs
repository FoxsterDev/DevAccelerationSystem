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

        private T New<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _created.Add(asset);
            return asset;
        }

        private LocalizationLocaleSet LocaleSet(params string[] codes)
        {
            var set = New<LocalizationLocaleSet>();
            set.Languages = new List<LocalizationLocaleProfile>();
            foreach (var code in codes)
            {
                set.Languages.Add(new LocalizationLocaleProfile { LanguageCode = code, DisplayName = code });
            }

            return set;
        }

        private LocalizationTextTable Table(string group, params (string key, string value)[] entries)
        {
            var table = New<LocalizationTextTable>();
            table.Group = group;
            table.Entries = new List<LocalizationEntry>();
            foreach (var (key, value) in entries)
            {
                table.Entries.Add(new LocalizationEntry { Key = key, EnglishFallback = value });
            }

            return table;
        }

        [Test]
        public void ValidCatalog_PassesValidation()
        {
            var catalog = New<LocalizationCatalog>();
            catalog.Locales = LocaleSet(LocalizationLanguageCodes.English, LocalizationLanguageCodes.BrazilianPortuguese);
            catalog.TextTables = new List<LocalizationTextTable>
            {
                Table("options", ("options.title", "Options")),
                Table("shop", ("shop.title", "Buy"))
            };

            Assert.IsTrue(catalog.IsValid(out var error), error);
        }

        [Test]
        public void Catalog_WithoutLocaleSet_IsInvalid()
        {
            var catalog = New<LocalizationCatalog>();
            catalog.Locales = null;

            Assert.IsFalse(catalog.IsValid(out _));
        }

        [Test]
        public void Catalog_WithoutEnglish_IsInvalid()
        {
            var catalog = New<LocalizationCatalog>();
            catalog.Locales = LocaleSet(LocalizationLanguageCodes.BrazilianPortuguese);

            Assert.IsFalse(catalog.IsValid(out _));
        }

        [Test]
        public void LocaleSet_WithDuplicateLanguageCode_IsInvalid()
        {
            var set = LocaleSet(LocalizationLanguageCodes.English, LocalizationLanguageCodes.English);

            Assert.IsFalse(set.Validate(out _));
        }

        [Test]
        public void TextTable_WithDuplicateKey_IsInvalid()
        {
            var table = Table("dup", ("dup", "1"), ("dup", "2"));

            Assert.IsFalse(table.Validate(out _));
        }

        [Test]
        public void Catalog_WithDuplicateKeyAcrossTables_IsInvalid()
        {
            var catalog = New<LocalizationCatalog>();
            catalog.Locales = LocaleSet(LocalizationLanguageCodes.English);
            catalog.TextTables = new List<LocalizationTextTable>
            {
                Table("a", ("shared.key", "1")),
                Table("b", ("shared.key", "2"))
            };

            Assert.IsFalse(catalog.IsValid(out _));
        }

        [Test]
        public void Catalog_TryGetEntry_SearchesAcrossTables()
        {
            var catalog = New<LocalizationCatalog>();
            catalog.Locales = LocaleSet(LocalizationLanguageCodes.English);
            catalog.TextTables = new List<LocalizationTextTable>
            {
                Table("a", ("a.key", "A")),
                Table("b", ("b.key", "B"))
            };

            Assert.IsTrue(catalog.TryGetEntry("b.key", out var entry));
            Assert.AreEqual("B", entry.EnglishFallback);
            Assert.IsFalse(catalog.TryGetEntry("missing.key", out _));
        }

        [Test]
        public void Catalog_WithNullTable_IsTolerated()
        {
            var catalog = New<LocalizationCatalog>();
            catalog.Locales = LocaleSet(LocalizationLanguageCodes.English);
            catalog.TextTables = new List<LocalizationTextTable>
            {
                null,
                Table("a", ("a.key", "A"))
            };

            Assert.IsTrue(catalog.IsValid(out var error), error);
            Assert.IsTrue(catalog.TryGetEntry("a.key", out _));
        }

        [Test]
        public void FontProfileLookup_ReturnsLocaleFont()
        {
            var primary = New<TMP_FontAsset>();
            var set = New<LocalizationLocaleSet>();
            set.Languages = new List<LocalizationLocaleProfile>
            {
                new()
                {
                    LanguageCode = LocalizationLanguageCodes.English,
                    FontProfile = new LocalizationFontProfile { PrimaryFont = primary }
                }
            };
            var catalog = New<LocalizationCatalog>();
            catalog.Locales = set;

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
