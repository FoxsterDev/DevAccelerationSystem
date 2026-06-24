using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Loqui.Tests
{
    [TestFixture]
    public class LanguagePickerControllerTests
    {
        private readonly List<Object> _created = new();
        private LanguagePickerController _controller;

        [SetUp]
        public void SetUp()
        {
            LocalizationPreferences.ClearExplicitChoice();
            var catalog = LocalizationTestAssets.Catalog(_created);
            var settings = LocalizationTestAssets.Settings(catalog, true);
            Loc.Initialize(settings, SystemLanguage.English, LocalizationPlatform.Default);
            _controller = new LanguagePickerController();
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
                    Object.DestroyImmediate(obj);
                }
            }

            _created.Clear();
        }

        [Test]
        public void Options_ExposeEnabledLanguages()
        {
            Assert.GreaterOrEqual(_controller.Options.Count, 2);
        }

        [Test]
        public void IsAvailable_TrueWhenMultipleLanguagesReady()
        {
            Assert.IsTrue(_controller.IsAvailable);
        }

        [Test]
        public void CurrentIndex_ReflectsActiveLanguage()
        {
            var index = _controller.CurrentIndex;

            Assert.AreEqual(LocalizationLanguageCodes.English, _controller.Options[index].LanguageCode);
        }

        [Test]
        public void SelectIndex_SwitchesActiveLanguage()
        {
            var target = -1;
            for (var i = 0; i < _controller.Options.Count; i++)
            {
                if (LocalizationLanguageCodes.Equals(_controller.Options[i].LanguageCode, LocalizationLanguageCodes.BrazilianPortuguese))
                {
                    target = i;
                    break;
                }
            }

            Assert.AreNotEqual(-1, target);
            Assert.IsTrue(_controller.SelectIndex(target));
            Assert.IsTrue(LocalizationLanguageCodes.Equals(LocalizationLanguageCodes.BrazilianPortuguese, _controller.CurrentLanguageCode));
        }

        [Test]
        public void SelectIndex_RejectsOutOfRange()
        {
            Assert.IsFalse(_controller.SelectIndex(-1));
            Assert.IsFalse(_controller.SelectIndex(_controller.Options.Count));
        }
    }
}
