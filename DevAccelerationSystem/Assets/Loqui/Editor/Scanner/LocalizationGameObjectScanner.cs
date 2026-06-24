using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Loqui.Editor
{
    public static class LocalizationGameObjectScanner
    {
        public static void Collect(
            GameObject root,
            string assetPath,
            string containerName,
            string containerKind,
            string group,
            List<LocalizationScanItem> buffer)
        {
            if (root == null || buffer == null)
            {
                return;
            }

            var tmpComponents = root.GetComponentsInChildren<TMP_Text>(true);
            for (var i = 0; i < tmpComponents.Length; i++)
            {
                var component = tmpComponents[i];
                if (component is TMP_InputField)
                {
                    continue;
                }

                buffer.Add(Build(
                    component,
                    component.text,
                    LocalizationScanSource.TmpText,
                    assetPath,
                    containerName,
                    containerKind,
                    group,
                    root.transform));
            }

            var legacyComponents = root.GetComponentsInChildren<Text>(true);
            for (var i = 0; i < legacyComponents.Length; i++)
            {
                var component = legacyComponents[i];
                buffer.Add(Build(
                    component,
                    component.text,
                    LocalizationScanSource.LegacyText,
                    assetPath,
                    containerName,
                    containerKind,
                    group,
                    root.transform));
            }
        }

        private static LocalizationScanItem Build(
            Component component,
            string source,
            LocalizationScanSource sourceKind,
            string assetPath,
            string containerName,
            string containerKind,
            string group,
            Transform root)
        {
            var item = new LocalizationScanItem
            {
                Source = sourceKind,
                AssetPath = assetPath,
                ContainerName = containerName,
                ContainerKind = containerKind,
                HierarchyPath = BuildHierarchyPath(component.transform, root),
                ComponentType = sourceKind == LocalizationScanSource.TmpText ? "TMP_Text" : "Text",
                TextComponentId = component.gameObject.name,
                EnglishSource = source,
                Group = group,
                PlatformDefault = source,
                RecommendedApproach = LocalizationRecommendedApproaches.ComponentAttach
            };

            if (component.GetComponent<LocalizedText>() != null)
            {
                item.ExclusionReason = "Already has LocalizedText";
                item.RecommendedApproach = LocalizationRecommendedApproaches.Exclude;
            }
            else if (string.IsNullOrWhiteSpace(source))
            {
                item.ExclusionReason = "Empty text";
                item.RecommendedApproach = LocalizationRecommendedApproaches.Exclude;
            }
            else
            {
                item.IsCandidate = true;
                item.ProposedKey = LocalizationKeyGenerator.Generate(group, source);
            }

            return item;
        }

        private static string BuildHierarchyPath(Transform target, Transform root)
        {
            var builder = new StringBuilder();
            var current = target;
            while (current != null)
            {
                if (builder.Length > 0)
                {
                    builder.Insert(0, '/');
                }

                builder.Insert(0, current.name);
                if (current == root)
                {
                    break;
                }

                current = current.parent;
            }

            return builder.ToString();
        }
    }
}
