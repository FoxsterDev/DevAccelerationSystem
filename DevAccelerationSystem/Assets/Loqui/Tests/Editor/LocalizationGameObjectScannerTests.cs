using System.Collections.Generic;
using Loqui.Editor;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Loqui.Tests
{
    [TestFixture]
    public class LocalizationGameObjectScannerTests
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

        private GameObject BuildFixture()
        {
            var root = new GameObject("Panel");
            _created.Add(root);

            var tmpGo = new GameObject("Title");
            tmpGo.transform.SetParent(root.transform);
            tmpGo.AddComponent<TextMeshPro>().text = "Play Now";

            var legacyGo = new GameObject("Subtitle");
            legacyGo.transform.SetParent(root.transform);
            legacyGo.AddComponent<Text>().text = "Tap to start";

            var emptyGo = new GameObject("Empty");
            emptyGo.transform.SetParent(root.transform);
            emptyGo.AddComponent<TextMeshPro>().text = "   ";

            var alreadyGo = new GameObject("Localized");
            alreadyGo.transform.SetParent(root.transform);
            alreadyGo.AddComponent<TextMeshPro>().text = "Settings";
            alreadyGo.AddComponent<LocalizedText>();

            return root;
        }

        [Test]
        public void Collect_FindsTmpAndLegacyTextAsCandidates()
        {
            var root = BuildFixture();
            var items = new List<LocalizationScanItem>();

            LocalizationGameObjectScanner.Collect(root, "Assets/Panel.prefab", "Panel", "Prefab", "panel", items);

            var tmp = items.Find(i => i.TextComponentId == "Title");
            var legacy = items.Find(i => i.TextComponentId == "Subtitle");
            Assert.IsNotNull(tmp);
            Assert.IsNotNull(legacy);
            Assert.AreEqual("TMP_Text", tmp.ComponentType);
            Assert.AreEqual("Text", legacy.ComponentType);
            Assert.IsTrue(tmp.IsCandidate);
            Assert.IsTrue(legacy.IsCandidate);
            Assert.AreEqual(LocalizationRecommendedApproaches.ComponentAttach, tmp.RecommendedApproach);
            Assert.AreEqual(LocalizationRecommendedApproaches.ComponentAttach, legacy.RecommendedApproach);
            Assert.AreEqual("panel.play_now", tmp.ProposedKey);
            Assert.AreEqual("Panel/Subtitle", legacy.HierarchyPath);
        }

        [Test]
        public void Collect_ExcludesEmptyText()
        {
            var root = BuildFixture();
            var items = new List<LocalizationScanItem>();

            LocalizationGameObjectScanner.Collect(root, "Assets/Panel.prefab", "Panel", "Prefab", "panel", items);

            var empty = items.Find(i => i.TextComponentId == "Empty");
            Assert.IsNotNull(empty);
            Assert.IsFalse(empty.IsCandidate);
            Assert.AreEqual("Empty text", empty.ExclusionReason);
            Assert.AreEqual(LocalizationRecommendedApproaches.Exclude, empty.RecommendedApproach);
        }

        [Test]
        public void Collect_ExcludesAlreadyLocalizedText()
        {
            var root = BuildFixture();
            var items = new List<LocalizationScanItem>();

            LocalizationGameObjectScanner.Collect(root, "Assets/Panel.prefab", "Panel", "Prefab", "panel", items);

            var already = items.Find(i => i.TextComponentId == "Localized");
            Assert.IsNotNull(already);
            Assert.IsFalse(already.IsCandidate);
            Assert.AreEqual("Already has LocalizedText", already.ExclusionReason);
            Assert.AreEqual(LocalizationRecommendedApproaches.Exclude, already.RecommendedApproach);
        }
    }
}
