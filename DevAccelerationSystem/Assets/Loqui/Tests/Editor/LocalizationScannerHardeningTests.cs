using System.Collections.Generic;
using Loqui.Editor;
using NUnit.Framework;

namespace Loqui.Tests
{
    [TestFixture]
    public class LocalizationScannerHardeningTests
    {
        [Test]
        public void CSharpScanner_IgnoresCommentedOutAssignments()
        {
            var source = "// label.text = \"Old\";\n/* hidden.SetText(\"Block\"); */\nreal.text = \"Live\";";
            var items = new List<LocalizationScanItem>();

            LocalizationCSharpScanner.ExtractCandidates(source, "Assets/Foo.cs", items);

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("Live", items[0].EnglishSource);
        }

        [Test]
        public void CSharpScanner_KeepsDoubleSlashInsideStringLiteral()
        {
            var source = "link.text = \"http://example.com\";";
            var items = new List<LocalizationScanItem>();

            LocalizationCSharpScanner.ExtractCandidates(source, "Assets/Foo.cs", items);

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("http://example.com", items[0].EnglishSource);
        }

        [Test]
        public void DisambiguateKeys_OrganicKeyCollidingWithSynthesizedSuffix_StaysUnique()
        {
            var items = new List<LocalizationScanItem>
            {
                Item("Assets/A.prefab", "A/1", "play"),
                Item("Assets/B.prefab", "B/1", "play"),
                Item("Assets/C.prefab", "C/1", "play_2")
            };

            var finalized = LocalizationTextScanner.Finalize(items);

            var keys = new HashSet<string>();
            foreach (var item in finalized)
            {
                Assert.IsTrue(keys.Add(item.ProposedKey), "Duplicate key after disambiguation: " + item.ProposedKey);
            }
        }

        [Test]
        public void Finalize_SiblingsWithIdenticalSortKeys_OrderByTextComponentId()
        {
            var items = new List<LocalizationScanItem>
            {
                Sibling("Title2"),
                Sibling("Title1")
            };

            var finalized = LocalizationTextScanner.Finalize(items);

            Assert.AreEqual("Title1", finalized[0].TextComponentId);
            Assert.AreEqual("Title2", finalized[1].TextComponentId);
        }

        private static LocalizationScanItem Sibling(string componentId)
        {
            return new LocalizationScanItem
            {
                AssetPath = "Assets/Panel.prefab",
                HierarchyPath = "Panel/Row",
                ComponentType = "TMP_Text",
                EnglishSource = "Play",
                TextComponentId = componentId,
                ProposedKey = componentId.ToLowerInvariant(),
                RecommendedApproach = LocalizationRecommendedApproaches.ComponentAttach,
                IsCandidate = true
            };
        }

        private static LocalizationScanItem Item(string assetPath, string hierarchy, string key)
        {
            return new LocalizationScanItem
            {
                AssetPath = assetPath,
                HierarchyPath = hierarchy,
                ComponentType = "TMP_Text",
                EnglishSource = "Play",
                ProposedKey = key,
                RecommendedApproach = LocalizationRecommendedApproaches.ComponentAttach,
                IsCandidate = true
            };
        }
    }
}
