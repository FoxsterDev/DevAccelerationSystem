using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Loqui.Editor
{
    [Serializable]
    public sealed class LocalizationMutationRecord
    {
        public string AssetPath;
        public string HierarchyPath;
        public string Key;
        public string Action;
    }

    public static class LocalizationAttachMode
    {
        public const string AttachedAction = "AttachedLocalizedText";
        public const string SkippedExistingAction = "Skipped:AlreadyPresent";
        public const string SkippedRecommendedApproachAction = "Skipped:RecommendedApproach";

        public static bool TryAttach(GameObject node, string key, string fallback, out LocalizationMutationRecord record)
        {
            record = new LocalizationMutationRecord { Key = key };
            if (node == null)
            {
                record.Action = "Skipped:NullNode";
                return false;
            }

            if (node.GetComponent<LocalizedText>() != null)
            {
                record.Action = SkippedExistingAction;
                return false;
            }

            var localized = node.AddComponent<LocalizedText>();
            var serialized = new SerializedObject(localized);
            serialized.FindProperty("_key").stringValue = key;
            serialized.FindProperty("_fallback").stringValue = fallback;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            record.Action = AttachedAction;
            return true;
        }

        public static List<LocalizationMutationRecord> AttachApproved(IReadOnlyList<LocalizationScanItem> approved)
        {
            var report = new List<LocalizationMutationRecord>();
            if (approved == null)
            {
                return report;
            }

            var byScene = new Dictionary<string, List<LocalizationScanItem>>(StringComparer.Ordinal);
            var byPrefab = new Dictionary<string, List<LocalizationScanItem>>(StringComparer.Ordinal);
            foreach (var item in approved)
            {
                if (item == null || !item.IsCandidate || string.IsNullOrEmpty(item.AssetPath))
                {
                    continue;
                }

                if (!CanAttachLocalizedText(item))
                {
                    report.Add(new LocalizationMutationRecord
                    {
                        AssetPath = item.AssetPath,
                        HierarchyPath = item.HierarchyPath,
                        Key = item.ProposedKey,
                        Action = SkippedRecommendedApproachAction + ":" + item.RecommendedApproach
                    });
                    continue;
                }

                var target = string.Equals(item.ContainerKind, "Prefab", StringComparison.Ordinal) ? byPrefab : byScene;
                if (!target.TryGetValue(item.AssetPath, out var list))
                {
                    list = new List<LocalizationScanItem>();
                    target[item.AssetPath] = list;
                }

                list.Add(item);
            }

            foreach (var pair in byPrefab)
            {
                AttachInPrefab(pair.Key, pair.Value, report);
            }

            foreach (var pair in byScene)
            {
                AttachInScene(pair.Key, pair.Value, report);
            }

            return report;
        }

        private static bool CanAttachLocalizedText(LocalizationScanItem item)
        {
            return string.Equals(item.RecommendedApproach, LocalizationRecommendedApproaches.ComponentAttach, StringComparison.Ordinal) &&
                   (string.Equals(item.ContainerKind, "Prefab", StringComparison.Ordinal) ||
                    string.Equals(item.ContainerKind, "Scene", StringComparison.Ordinal));
        }

        private static void AttachInPrefab(string path, List<LocalizationScanItem> items, List<LocalizationMutationRecord> report)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            var changed = false;
            try
            {
                foreach (var item in items)
                {
                    var node = FindByHierarchyPath(root.transform, item.HierarchyPath);
                    if (TryAttach(node, item.ProposedKey, item.EnglishSource, out var record))
                    {
                        changed = true;
                    }

                    record.AssetPath = path;
                    record.HierarchyPath = item.HierarchyPath;
                    report.Add(record);
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AttachInScene(string path, List<LocalizationScanItem> items, List<LocalizationMutationRecord> report)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            var changed = false;
            try
            {
                var roots = scene.GetRootGameObjects();
                foreach (var item in items)
                {
                    var node = FindInRoots(roots, item.HierarchyPath);
                    if (TryAttach(node, item.ProposedKey, item.EnglishSource, out var record))
                    {
                        changed = true;
                    }

                    record.AssetPath = path;
                    record.HierarchyPath = item.HierarchyPath;
                    report.Add(record);
                }

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject FindInRoots(GameObject[] roots, string hierarchyPath)
        {
            if (string.IsNullOrEmpty(hierarchyPath))
            {
                return null;
            }

            var slash = hierarchyPath.IndexOf('/');
            var rootName = slash < 0 ? hierarchyPath : hierarchyPath.Substring(0, slash);
            foreach (var root in roots)
            {
                if (!string.Equals(root.name, rootName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (slash < 0)
                {
                    return root;
                }

                return FindByRelativePath(root.transform, hierarchyPath.Substring(slash + 1));
            }

            return null;
        }

        public static GameObject FindByHierarchyPath(Transform root, string hierarchyPath)
        {
            if (root == null || string.IsNullOrEmpty(hierarchyPath))
            {
                return null;
            }

            var slash = hierarchyPath.IndexOf('/');
            var rootName = slash < 0 ? hierarchyPath : hierarchyPath.Substring(0, slash);
            if (!string.Equals(root.name, rootName, StringComparison.Ordinal))
            {
                return null;
            }

            if (slash < 0)
            {
                return root.gameObject;
            }

            return FindByRelativePath(root, hierarchyPath.Substring(slash + 1));
        }

        private static GameObject FindByRelativePath(Transform root, string relativePath)
        {
            var current = root;
            var segments = relativePath.Split('/');
            foreach (var segment in segments)
            {
                current = current.Find(segment);
                if (current == null)
                {
                    return null;
                }
            }

            return current.gameObject;
        }
    }
}
