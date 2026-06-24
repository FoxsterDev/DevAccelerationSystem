using System.Collections.Generic;
using System.Reflection;
using Loqui;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Loqui.Tests
{
    [TestFixture]
    public class LocalizedTextTests
    {
        private readonly List<Object> _created = new();
        private readonly List<LocalizedText> _components = new();

        [SetUp]
        public void SetUp()
        {
            LocalizationPreferences.ClearExplicitChoice();
            var catalog = LocalizationTestAssets.Catalog(_created);
            var settings = LocalizationTestAssets.Settings(catalog, true);
            Loc.Initialize(settings, SystemLanguage.English, LocalizationPlatform.Default);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var component in _components)
            {
                if (component != null)
                {
                    component.HandleDisable();
                }
            }

            _components.Clear();
            Loc.Shutdown();
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

        private (LocalizedText component, TMP_Text label) BuildLabel(string key, string fallback)
        {
            var go = new GameObject("LocalizedText");
            _created.Add(go);
            var label = go.AddComponent<TextMeshPro>();
            var component = go.AddComponent<LocalizedText>();
            _components.Add(component);
            SetField(component, "_key", key);
            SetField(component, "_fallback", fallback);
            return (component, label);
        }

        private static void SetField(object target, string name, string value)
        {
            var field = typeof(LocalizedText).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        [Test]
        public void Enable_AppliesActiveLanguage()
        {
            var (component, label) = BuildLabel("greeting", "FB");

            component.HandleEnable();

            Assert.AreEqual("Hello", label.text);
        }

        [Test]
        public void LanguageChange_RefreshesLabelWhileEnabled()
        {
            var (component, label) = BuildLabel("greeting", "FB");
            component.HandleEnable();

            Assert.IsTrue(Loc.SetLanguage(LocalizationLanguageCodes.BrazilianPortuguese));

            Assert.AreEqual("Olá", label.text);
        }

        [Test]
        public void DisabledComponent_DoesNotRefreshOnLanguageChange()
        {
            var (component, label) = BuildLabel("greeting", "FB");
            component.HandleEnable();
            component.HandleDisable();

            Loc.SetLanguage(LocalizationLanguageCodes.BrazilianPortuguese);

            Assert.AreEqual("Hello", label.text);
        }

        [Test]
        public void CacheTargets_AutoAssignsTextComponentOnSameObject()
        {
            var (component, label) = BuildLabel("greeting", "FB");

            component.CacheTargets();

            var field = typeof(LocalizedText).GetField("_tmpTarget", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.AreSame(label, field.GetValue(component));
        }

        [Test]
        public void MissingKey_UsesInspectorFallback()
        {
            var (component, label) = BuildLabel("does.not.exist", "Design Fallback");

            component.HandleEnable();

            Assert.AreEqual("Design Fallback", label.text);
        }
    }
}
