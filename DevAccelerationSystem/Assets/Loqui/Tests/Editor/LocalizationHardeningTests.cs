using System;
using System.Collections.Generic;
using Loqui.Remote;
using NUnit.Framework;
using UnityEngine;

namespace Loqui.Tests
{
    [TestFixture]
    public class LocalizationHardeningTests
    {
        private readonly List<UnityEngine.Object> _created = new();

        [SetUp]
        public void SetUp()
        {
            LocalizationPreferences.ClearExplicitChoice();
        }

        [TearDown]
        public void TearDown()
        {
            Loc.Shutdown();
            LocalizationPreferences.ClearExplicitChoice();
            foreach (var obj in _created)
            {
                if (obj != null)
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }

            _created.Clear();
        }

        private LocalizationService EnabledService(LocalizationPlatform platform = LocalizationPlatform.Default)
        {
            var catalog = LocalizationTestAssets.Catalog(_created);
            var settings = LocalizationTestAssets.Settings(catalog, true);
            var service = new LocalizationService(settings, SystemLanguage.English, platform);
            service.Initialize();
            return service;
        }

        [Test]
        public void Ready_FiresImmediately_ForLateSubscriber()
        {
            var catalog = LocalizationTestAssets.Catalog(_created);
            var settings = LocalizationTestAssets.Settings(catalog, true);
            Loc.Initialize(settings, SystemLanguage.English, LocalizationPlatform.Default);
            Assert.IsTrue(Loc.IsReady);

            var fired = 0;
            Action handler = () => fired++;
            Loc.Ready += handler;
            try
            {
                Assert.AreEqual(1, fired, "Late Ready subscriber should fire immediately exactly once.");
            }
            finally
            {
                Loc.Ready -= handler;
            }
        }

        [Test]
        public void Ready_FiresOnce_ForEarlySubscriber()
        {
            var catalog = LocalizationTestAssets.Catalog(_created);
            var settings = LocalizationTestAssets.Settings(catalog, true);

            var fired = 0;
            Action handler = () => fired++;
            Loc.Ready += handler;
            try
            {
                Loc.Initialize(settings, SystemLanguage.English, LocalizationPlatform.Default);
                Assert.AreEqual(1, fired, "Early Ready subscriber should fire exactly once at Initialize.");
            }
            finally
            {
                Loc.Ready -= handler;
            }
        }

        [Test]
        public void Raise_ReentrantFromListener_DoesNotCorruptOuterDispatch()
        {
            var evt = new LocalizationEvent();
            var calls = new int[3];
            var reentered = false;
            Action l0 = null;
            l0 = () =>
            {
                calls[0]++;
                if (!reentered)
                {
                    reentered = true;
                    evt.Raise();
                }
            };
            evt.Add(l0);
            evt.Add(() => calls[1]++);
            evt.Add(() => calls[2]++);

            Assert.DoesNotThrow(() => evt.Raise());

            Assert.AreEqual(2, calls[1], "Outer pass must still deliver l1 after a nested raise.");
            Assert.AreEqual(2, calls[2], "Outer pass must still deliver l2 after a nested raise.");
        }

        [Test]
        public void ApplyOverrides_AppliesAndSurvivesLanguageSwitch()
        {
            var service = EnabledService();
            Assert.IsTrue(service.TryGet("greeting", out var baseValue));
            Assert.AreEqual("Hello", baseValue);

            Assert.IsTrue(service.ApplyOverrides(Overrides(("en", "greeting", "HelloOverride"))));
            Assert.IsTrue(service.TryGet("greeting", out var overridden));
            Assert.AreEqual("HelloOverride", overridden);

            Assert.IsTrue(service.SetLanguage(LocalizationLanguageCodes.BrazilianPortuguese));
            Assert.IsTrue(service.TryGet("greeting", out var pt));
            Assert.AreEqual("Olá", pt, "Override only targets the en block.");

            Assert.IsTrue(service.SetLanguage(LocalizationLanguageCodes.English));
            Assert.IsTrue(service.TryGet("greeting", out var again));
            Assert.AreEqual("HelloOverride", again, "Override should survive a language switch.");

            Assert.IsTrue(service.ClearOverrides());
            Assert.IsTrue(service.TryGet("greeting", out var cleared));
            Assert.AreEqual("Hello", cleared);
        }

        [Test]
        public void ApplyOverrides_iOSField_MapsToIOSPlatform()
        {
            var service = EnabledService(LocalizationPlatform.IOS);

            var dto = new LocalizationOverridesDto
            {
                SchemaVersion = 1,
                Languages = new[]
                {
                    new LocalizationOverrideLanguageDto
                    {
                        LanguageCode = "en",
                        Entries = new[]
                        {
                            new LocalizationOverrideEntryDto { Key = "greeting", Default = "D", iOS = "FromIOS" }
                        }
                    }
                }
            };

            Assert.IsTrue(service.ApplyOverrides(new LocalizationOverridesResult { Accepted = true, Payload = dto }));
            Assert.IsTrue(service.TryGet("greeting", out var value));
            Assert.AreEqual("FromIOS", value);
        }

        [Test]
        public void ApplyOverrides_RejectedResult_IsIgnored()
        {
            var service = EnabledService();
            Assert.IsFalse(service.ApplyOverrides(LocalizationOverridesResult.Reject("nope")));
            Assert.IsFalse(service.ApplyOverrides(null));
            Assert.IsTrue(service.TryGet("greeting", out var value));
            Assert.AreEqual("Hello", value);
        }

        [Test]
        public void SetLanguage_ToCurrentLanguage_IsNoOp_NoEvent()
        {
            var service = EnabledService();
            Assert.AreEqual(LocalizationLanguageCodes.English, service.CurrentLanguageCode);

            var raised = 0;
            service.LanguageChanged += () => raised++;

            Assert.IsTrue(service.SetLanguage(LocalizationLanguageCodes.English));
            Assert.AreEqual(0, raised, "Re-selecting the active language must not raise LanguageChanged.");
            Assert.AreEqual(LocalizationLanguageCodes.English, service.CurrentLanguageCode);
        }

        [Test]
        public void Initialize_RestoresPersistedLanguageChoice()
        {
            LocalizationPreferences.SetExplicitChoice(LocalizationLanguageCodes.BrazilianPortuguese);
            var catalog = LocalizationTestAssets.Catalog(_created);
            var settings = LocalizationTestAssets.Settings(catalog, true);
            var service = new LocalizationService(settings, SystemLanguage.English, LocalizationPlatform.Default);

            service.Initialize();

            Assert.AreEqual(LocalizationLanguageCodes.BrazilianPortuguese, service.CurrentLanguageCode);
        }

        [Test]
        public void Initialize_StalePersistedChoice_FallsBackSafely()
        {
            LocalizationPreferences.SetExplicitChoice("xx-XX");
            var catalog = LocalizationTestAssets.Catalog(_created);
            var settings = LocalizationTestAssets.Settings(catalog, true);
            var service = new LocalizationService(settings, SystemLanguage.English, LocalizationPlatform.Default);

            Assert.DoesNotThrow(() => service.Initialize());
            Assert.AreEqual(LocalizationLanguageCodes.English, service.CurrentLanguageCode);
        }

        [Test]
        public void Raise_AfterMidListRemove_DeliversToRemainingListenersInSwapOrder()
        {
            var evt = new LocalizationEvent();
            var order = new List<string>();
            Action a = () => order.Add("a");
            Action b = () => order.Add("b");
            Action c = () => order.Add("c");
            evt.Add(a);
            evt.Add(b);
            evt.Add(c);
            evt.Remove(b);

            evt.Raise();

            Assert.AreEqual(2, evt.Count);
            Assert.AreEqual(new[] { "a", "c" }, order.ToArray());
        }

        private static LocalizationOverridesResult Overrides(params (string lang, string key, string value)[] entries)
        {
            var byLang = new Dictionary<string, List<LocalizationOverrideEntryDto>>();
            foreach (var (lang, key, value) in entries)
            {
                if (!byLang.TryGetValue(lang, out var list))
                {
                    list = new List<LocalizationOverrideEntryDto>();
                    byLang[lang] = list;
                }

                list.Add(new LocalizationOverrideEntryDto { Key = key, Default = value });
            }

            var languages = new List<LocalizationOverrideLanguageDto>();
            foreach (var pair in byLang)
            {
                languages.Add(new LocalizationOverrideLanguageDto
                {
                    LanguageCode = pair.Key,
                    Entries = pair.Value.ToArray()
                });
            }

            var dto = new LocalizationOverridesDto
            {
                SchemaVersion = 1,
                Languages = languages.ToArray()
            };
            return new LocalizationOverridesResult { Accepted = true, Payload = dto };
        }
    }
}
