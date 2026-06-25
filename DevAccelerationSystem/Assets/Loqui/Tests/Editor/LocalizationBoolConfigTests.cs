using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Loqui.Tests
{
    [TestFixture]
    public class LocalizationBoolConfigTests
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

        [Test]
        public void BoolValues_Resolve_UsesDefault_WhenOverrideInherit()
        {
            var values = new LocalizationBoolValues { Default = true };

            Assert.IsTrue(values.Resolve(LocalizationPlatform.Default));
            Assert.IsTrue(values.Resolve(LocalizationPlatform.IOS));
            Assert.IsTrue(values.Resolve(LocalizationPlatform.Android));
        }

        [Test]
        public void BoolValues_Resolve_AppliesPlatformOverride()
        {
            var values = new LocalizationBoolValues
            {
                Default = true,
                IOS = LocalizationBoolOverride.False,
                Android = LocalizationBoolOverride.Inherit
            };

            Assert.IsFalse(values.Resolve(LocalizationPlatform.IOS));
            Assert.IsTrue(values.Resolve(LocalizationPlatform.Android));
            Assert.IsTrue(values.Resolve(LocalizationPlatform.Default));
        }

        [Test]
        public void GetBool_ReturnsConfiguredValue_PerPlatform()
        {
            var service = ServiceWithFlag(LocalizationPlatform.IOS, defaultValue: true, iosOverride: LocalizationBoolOverride.False);

            Assert.IsTrue(service.TryGetBool("config.flag", out var value));
            Assert.IsFalse(value);
        }

        [Test]
        public void GetBool_FallsBack_WhenKeyMissing()
        {
            var service = ServiceWithFlag(LocalizationPlatform.Default, defaultValue: true, iosOverride: LocalizationBoolOverride.Inherit);

            Assert.IsFalse(service.TryGetBool("does.not.exist", out _));
            Assert.IsTrue(service.GetBool("does.not.exist", true));
            Assert.IsFalse(service.GetBool("does.not.exist", false));
        }

        [Test]
        public void GetBool_FallsBack_WhenDisabled()
        {
            var catalog = CatalogWithFlag(true, LocalizationBoolOverride.False);
            var settings = LocalizationTestAssets.Settings(catalog, enabled: false);
            var service = new LocalizationService(settings, SystemLanguage.English, LocalizationPlatform.IOS);
            service.Initialize();

            Assert.IsFalse(service.TryGetBool("config.flag", out _));
            Assert.IsTrue(service.GetBool("config.flag", true));
        }

        private LocalizationService ServiceWithFlag(LocalizationPlatform platform, bool defaultValue, LocalizationBoolOverride iosOverride)
        {
            var catalog = CatalogWithFlag(defaultValue, iosOverride);
            var settings = LocalizationTestAssets.Settings(catalog, enabled: true);
            var service = new LocalizationService(settings, SystemLanguage.English, platform);
            service.Initialize();
            return service;
        }

        private LocalizationCatalog CatalogWithFlag(bool defaultValue, LocalizationBoolOverride iosOverride)
        {
            var catalog = LocalizationTestAssets.Catalog(_created);
            catalog.Bools = new List<LocalizationBoolEntry>
            {
                new()
                {
                    Key = "config.flag",
                    Values = new LocalizationBoolValues
                    {
                        Default = defaultValue,
                        IOS = iosOverride,
                        Android = LocalizationBoolOverride.Inherit
                    }
                }
            };
            return catalog;
        }
    }
}
