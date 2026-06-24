using System.Collections.Generic;
using Loqui.Editor;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Loqui.Tests
{
    [TestFixture]
    public class LocalizationAttachModeTests
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

        private GameObject NewLabel(string name)
        {
            var go = new GameObject(name);
            _created.Add(go);
            go.AddComponent<TextMeshPro>().text = name;
            return go;
        }

        [Test]
        public void TryAttach_AddsLocalizedTextAndSetsFields()
        {
            var go = NewLabel("Title");

            var attached = LocalizationAttachMode.TryAttach(go, "panel.title", "Title", out var record);

            Assert.IsTrue(attached);
            Assert.AreEqual(LocalizationAttachMode.AttachedAction, record.Action);
            var component = go.GetComponent<LocalizedText>();
            Assert.IsNotNull(component);
            Assert.AreEqual("panel.title", component.Key);
        }

        [Test]
        public void TryAttach_IsIdempotentForExistingComponent()
        {
            var go = NewLabel("Title");
            LocalizationAttachMode.TryAttach(go, "panel.title", "Title", out _);

            var second = LocalizationAttachMode.TryAttach(go, "panel.title", "Title", out var record);

            Assert.IsFalse(second);
            Assert.AreEqual(LocalizationAttachMode.SkippedExistingAction, record.Action);
            Assert.AreEqual(1, go.GetComponents<LocalizedText>().Length);
        }

        [Test]
        public void AttachApproved_OnlyTouchesApprovedNodesAndReportsEach()
        {
            var approved = NewLabel("Approved");
            var untouched = NewLabel("Untouched");

            LocalizationAttachMode.TryAttach(approved, "panel.approved", "Approved", out var record);

            Assert.IsNotNull(approved.GetComponent<LocalizedText>());
            Assert.IsNull(untouched.GetComponent<LocalizedText>());
            Assert.AreEqual(LocalizationAttachMode.AttachedAction, record.Action);
        }

        [Test]
        public void AttachApproved_SkipsCodeApiRecommendations()
        {
            var report = LocalizationAttachMode.AttachApproved(new[]
            {
                new LocalizationScanItem
                {
                    AssetPath = "Assets/Foo.cs",
                    ContainerKind = "Script",
                    ProposedKey = "foo.title",
                    RecommendedApproach = LocalizationRecommendedApproaches.CodeApi,
                    IsCandidate = true
                }
            });

            Assert.AreEqual(1, report.Count);
            Assert.AreEqual(
                LocalizationAttachMode.SkippedRecommendedApproachAction + ":" + LocalizationRecommendedApproaches.CodeApi,
                report[0].Action);
        }
    }
}
