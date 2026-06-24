using System.Collections.Generic;
using Loqui.Editor;
using NUnit.Framework;

namespace Loqui.Tests
{
    [TestFixture]
    public class LocalizationScannerDeterminismTests
    {
        private static List<LocalizationScanItem> Sample()
        {
            return new List<LocalizationScanItem>
            {
                new()
                {
                    AssetPath = "Assets/B.prefab",
                    HierarchyPath = "B/Two",
                    ComponentType = "TMP_Text",
                    EnglishSource = "Play",
                    ProposedKey = "b.play",
                    RecommendedApproach = LocalizationRecommendedApproaches.ComponentAttach,
                    IsCandidate = true
                },
                new()
                {
                    AssetPath = "Assets/A.prefab",
                    HierarchyPath = "A/One",
                    ComponentType = "TMP_Text",
                    EnglishSource = "Play",
                    ProposedKey = "a.play",
                    RecommendedApproach = LocalizationRecommendedApproaches.ComponentAttach,
                    IsCandidate = true
                },
                new()
                {
                    AssetPath = "Assets/A.prefab",
                    HierarchyPath = "A/Two",
                    ComponentType = "TMP_Text",
                    EnglishSource = "Play",
                    ProposedKey = "a.play",
                    RecommendedApproach = LocalizationRecommendedApproaches.ComponentAttach,
                    IsCandidate = true
                }
            };
        }

        [Test]
        public void Finalize_SortsByAssetThenHierarchy()
        {
            var items = LocalizationTextScanner.Finalize(Sample());

            Assert.AreEqual("Assets/A.prefab", items[0].AssetPath);
            Assert.AreEqual("A/One", items[0].HierarchyPath);
            Assert.AreEqual("A/Two", items[1].HierarchyPath);
            Assert.AreEqual("Assets/B.prefab", items[2].AssetPath);
        }

        [Test]
        public void Finalize_DisambiguatesDuplicateKeysStably()
        {
            var items = LocalizationTextScanner.Finalize(Sample());

            Assert.AreEqual("a.play", items[0].ProposedKey);
            Assert.AreEqual("a.play_2", items[1].ProposedKey);
            Assert.AreEqual("b.play", items[2].ProposedKey);
        }

        [Test]
        public void Finalize_IsRepeatable()
        {
            var first = LocalizationTextScanner.Finalize(Sample());
            var second = LocalizationTextScanner.Finalize(Sample());

            Assert.AreEqual(first.Count, second.Count);
            for (var i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i].ProposedKey, second[i].ProposedKey);
                Assert.AreEqual(first[i].AssetPath, second[i].AssetPath);
                Assert.AreEqual(first[i].HierarchyPath, second[i].HierarchyPath);
            }
        }

        [Test]
        public void CSharpScanner_ExtractsTextAssignmentLiterals()
        {
            var source = "label.text = \"Hello\"; other.SetText(\"World\"); var x = \"ignored\";";
            var items = new List<LocalizationScanItem>();

            LocalizationCSharpScanner.ExtractCandidates(source, "Assets/Foo.cs", items);

            Assert.AreEqual(2, items.Count);
            CollectionAssert.Contains(new[] { items[0].EnglishSource, items[1].EnglishSource }, "Hello");
            CollectionAssert.Contains(new[] { items[0].EnglishSource, items[1].EnglishSource }, "World");
            Assert.IsTrue(items[0].IsCandidate);
            Assert.AreEqual(LocalizationRecommendedApproaches.CodeApi, items[0].RecommendedApproach);
            Assert.IsTrue(items[0].RequiresReview);
            Assert.IsFalse(string.IsNullOrEmpty(items[0].CodeMutatorHint));
        }

        [Test]
        public void CSharpScanner_ExtractsNonLiteralMutatorHints()
        {
            var source = "_titleText.text = title;";
            var items = new List<LocalizationScanItem>();

            LocalizationCSharpScanner.ExtractCandidates(source, "Assets/Foo.cs", items);

            Assert.AreEqual(1, items.Count);
            Assert.IsFalse(items[0].IsCandidate);
            Assert.AreEqual(LocalizationRecommendedApproaches.CodeApi, items[0].RecommendedApproach);
            Assert.AreEqual("_titleText", items[0].CodeMutatorHint);
            Assert.AreEqual("Advisory: code mutates text from a non-literal expression", items[0].ExclusionReason);
        }

        [Test]
        public void Finalize_MarksMatchingTextComponentAsCodeApi()
        {
            var items = new List<LocalizationScanItem>
            {
                new()
                {
                    Source = LocalizationScanSource.TmpText,
                    AssetPath = "Assets/Panel.prefab",
                    HierarchyPath = "Panel/Title",
                    TextComponentId = "Title",
                    ComponentType = "TMP_Text",
                    EnglishSource = "Play",
                    ProposedKey = "panel.play",
                    RecommendedApproach = LocalizationRecommendedApproaches.ComponentAttach,
                    IsCandidate = true
                },
                new()
                {
                    Source = LocalizationScanSource.CSharpLiteral,
                    AssetPath = "Assets/PanelPresenter.cs",
                    ComponentType = "CSharpLiteral",
                    EnglishSource = "Play",
                    ProposedKey = "panel.play",
                    RecommendedApproach = LocalizationRecommendedApproaches.CodeApi,
                    CodeMutatorHint = "_titleText",
                    MutationEvidence = "Assets/PanelPresenter.cs:12 _titleText",
                    IsCandidate = true
                }
            };

            var finalized = LocalizationTextScanner.Finalize(items);
            var component = finalized.Find(i => i.Source == LocalizationScanSource.TmpText);

            Assert.AreEqual(LocalizationRecommendedApproaches.CodeApi, component.RecommendedApproach);
            Assert.IsTrue(component.RequiresReview);
            StringAssert.Contains("PanelPresenter.cs:12", component.MutationEvidence);
        }

        [Test]
        public void Finalize_MarksExistingLocalizedTextWithMutatorAsConflict()
        {
            var items = new List<LocalizationScanItem>
            {
                new()
                {
                    Source = LocalizationScanSource.TmpText,
                    AssetPath = "Assets/Panel.prefab",
                    HierarchyPath = "Panel/Title",
                    TextComponentId = "Title",
                    ComponentType = "TMP_Text",
                    EnglishSource = "Play",
                    RecommendedApproach = LocalizationRecommendedApproaches.Exclude,
                    ExclusionReason = "Already has LocalizedText"
                },
                new()
                {
                    Source = LocalizationScanSource.CSharpLiteral,
                    AssetPath = "Assets/PanelPresenter.cs",
                    ComponentType = "CSharpLiteral",
                    RecommendedApproach = LocalizationRecommendedApproaches.CodeApi,
                    CodeMutatorHint = "_titleText",
                    MutationEvidence = "Assets/PanelPresenter.cs:12 _titleText"
                }
            };

            var finalized = LocalizationTextScanner.Finalize(items);
            var component = finalized.Find(i => i.Source == LocalizationScanSource.TmpText);

            Assert.AreEqual(LocalizationRecommendedApproaches.Conflict, component.RecommendedApproach);
            Assert.IsFalse(component.IsCandidate);
            StringAssert.Contains("Conflict", component.ExclusionReason);
        }
    }
}
