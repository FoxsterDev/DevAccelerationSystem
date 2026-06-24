using System.Collections;
using System.Collections.Generic;
using Loqui;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LoquiSample.Tests
{
    public class LoquiPlayModeTests
    {
        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            Loc.Shutdown();
            LocalizationPreferences.ClearExplicitChoice();
            foreach (var obj in _created)
            {
                if (obj != null)
                {
                    Object.Destroy(obj);
                }
            }

            _created.Clear();
        }

        [UnityTest]
        public IEnumerator LocalizedText_AppliesActiveLanguage_AndUpdatesOnSwitch()
        {
            InitLoc();
            var (_, tmp) = CreateLabel("greeting");
            yield return null;

            Assert.AreEqual("Hello", tmp.text);

            Assert.IsTrue(Loc.SetLanguage(LocalizationLanguageCodes.BrazilianPortuguese));
            yield return null;

            Assert.AreEqual("Olá", tmp.text);
        }

        [UnityTest]
        public IEnumerator LocalizedText_DisableReenable_ResubscribesAndReapplies()
        {
            InitLoc();
            var (go, tmp) = CreateLabel("greeting");
            yield return null;

            go.SetActive(false);
            yield return null;

            Assert.IsTrue(Loc.SetLanguage(LocalizationLanguageCodes.BrazilianPortuguese));
            yield return null;
            Assert.AreEqual("Hello", tmp.text, "Disabled label must not react to a language change.");

            go.SetActive(true);
            yield return null;
            Assert.AreEqual("Olá", tmp.text, "Re-enabled label must re-apply the active language.");
        }

        [UnityTest]
        public IEnumerator DestroyedLabel_DoesNotBreakLanguageSwitch()
        {
            InitLoc();
            var (go, _) = CreateLabel("greeting");
            yield return null;

            Object.Destroy(go);
            yield return null;

            Assert.DoesNotThrow(() => Loc.SetLanguage(LocalizationLanguageCodes.BrazilianPortuguese));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneReload_LeavesLocalizationUsable()
        {
            InitLoc();
            var (go, _) = CreateLabel("greeting");
            yield return null;

            var probe = SceneManager.CreateScene("LoquiReloadProbe");
            SceneManager.MoveGameObjectToScene(go, probe);
            yield return SceneManager.UnloadSceneAsync(probe);
            yield return null;

            Assert.IsTrue(Loc.IsReady);
            Assert.DoesNotThrow(() => Loc.SetLanguage(LocalizationLanguageCodes.BrazilianPortuguese));
        }

        private void InitLoc()
        {
            var settings = LoquiTestCatalog.Settings(_created);
            Loc.Initialize(settings, SystemLanguage.English, LocalizationPlatform.Default);
        }

        private (GameObject go, TMP_Text tmp) CreateLabel(string key)
        {
            var go = new GameObject("LoquiLabel");
            _created.Add(go);
            go.SetActive(false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            var label = go.AddComponent<LocalizedText>();
            label.Key = key;
            go.SetActive(true);
            return (go, tmp);
        }
    }
}
