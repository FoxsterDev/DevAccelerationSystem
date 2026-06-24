using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Loqui.Editor
{
    public sealed class LocalizationScanOptions
    {
        public string[] SearchFolders = { "Assets" };
        public bool IncludeScenes = true;
        public bool IncludePrefabs = true;
        public bool IncludeScripts = false;
    }

    public static class LocalizationTextScanner
    {
        private static readonly string[] NameSuffixes =
        {
            "textmeshpro",
            "textfield",
            "texts",
            "text",
            "label",
            "lbl",
            "tmp"
        };

        public static List<LocalizationScanItem> Scan(LocalizationScanOptions options)
        {
            options ??= new LocalizationScanOptions();
            var items = new List<LocalizationScanItem>();

            if (options.IncludePrefabs)
            {
                ScanPrefabs(options.SearchFolders, items);
            }

            if (options.IncludeScenes)
            {
                ScanScenes(options.SearchFolders, items);
            }

            if (options.IncludeScripts)
            {
                ScanScripts(options.SearchFolders, items);
            }

            return Finalize(items);
        }

        public static List<LocalizationScanItem> Finalize(List<LocalizationScanItem> items)
        {
            items.Sort(CompareItems);
            ApplyCodeMutatorRecommendations(items);
            DisambiguateKeys(items);
            return items;
        }

        private static void ScanPrefabs(string[] folders, List<LocalizationScanItem> items)
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", folders);
            var paths = ToSortedPaths(guids);
            foreach (var path in paths)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(path);
                    LocalizationGameObjectScanner.Collect(root, path, name, "Prefab", GroupFromPath(path), items);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static void ScanScenes(string[] folders, List<LocalizationScanItem> items)
        {
            var guids = AssetDatabase.FindAssets("t:Scene", folders);
            var paths = ToSortedPaths(guids);
            foreach (var path in paths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(path);
                    var group = GroupFromPath(path);
                    var roots = scene.GetRootGameObjects();
                    foreach (var root in roots)
                    {
                        LocalizationGameObjectScanner.Collect(root, path, name, "Scene", group, items);
                    }
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ScanScripts(string[] folders, List<LocalizationScanItem> items)
        {
            var guids = AssetDatabase.FindAssets("t:MonoScript", folders);
            var paths = ToSortedPaths(guids);
            foreach (var path in paths)
            {
                if (!path.EndsWith(".cs", StringComparison.Ordinal) || !System.IO.File.Exists(path))
                {
                    continue;
                }

                var source = System.IO.File.ReadAllText(path);
                LocalizationCSharpScanner.ExtractCandidates(source, path, items);
            }
        }

        private static string[] ToSortedPaths(string[] guids)
        {
            var paths = new string[guids.Length];
            for (var i = 0; i < guids.Length; i++)
            {
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            }

            Array.Sort(paths, StringComparer.Ordinal);
            return paths;
        }

        private static string GroupFromPath(string path)
        {
            return System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        }

        private static void DisambiguateKeys(List<LocalizationScanItem> items)
        {
            var seen = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var item in items)
            {
                if (!item.IsCandidate || string.IsNullOrEmpty(item.ProposedKey))
                {
                    continue;
                }

                if (seen.TryGetValue(item.ProposedKey, out var count))
                {
                    count++;
                    seen[item.ProposedKey] = count;
                    item.ProposedKey = item.ProposedKey + "_" + count.ToString();
                }
                else
                {
                    seen[item.ProposedKey] = 1;
                }
            }
        }

        private static void ApplyCodeMutatorRecommendations(List<LocalizationScanItem> items)
        {
            var mutatorsByToken = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (!string.Equals(item.RecommendedApproach, LocalizationRecommendedApproaches.CodeApi, StringComparison.Ordinal) ||
                    string.IsNullOrEmpty(item.CodeMutatorHint))
                {
                    continue;
                }

                var token = NormalizeNameForMatching(item.CodeMutatorHint);
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                if (!mutatorsByToken.TryGetValue(token, out var evidence))
                {
                    evidence = new List<string>();
                    mutatorsByToken[token] = evidence;
                }

                evidence.Add(item.MutationEvidence);
            }

            if (mutatorsByToken.Count == 0)
            {
                return;
            }

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.Source != LocalizationScanSource.TmpText && item.Source != LocalizationScanSource.LegacyText)
                {
                    continue;
                }

                if (!TryFindMutatorEvidence(item, mutatorsByToken, out var evidence))
                {
                    continue;
                }

                item.CodeMutatorHint = BuildTargetHint(item);
                item.MutationEvidence = evidence;
                item.RequiresReview = true;

                if (string.Equals(item.RecommendedApproach, LocalizationRecommendedApproaches.Exclude, StringComparison.Ordinal) &&
                    string.Equals(item.ExclusionReason, "Already has LocalizedText", StringComparison.Ordinal))
                {
                    item.RecommendedApproach = LocalizationRecommendedApproaches.Conflict;
                    item.IsCandidate = false;
                    item.ExclusionReason = "Conflict: LocalizedText exists but code mutator hint also found";
                    item.Notes = AppendNote(item.Notes, "Resolve to one owner. CodeApi wins unless the mutator is removed.");
                    continue;
                }

                if (string.Equals(item.ExclusionReason, "Empty text", StringComparison.Ordinal))
                {
                    item.RecommendedApproach = LocalizationRecommendedApproaches.CodeApi;
                    item.ExclusionReason = "Advisory: empty authored text has a code mutator hint";
                    item.Notes = AppendNote(
                        item.Notes,
                        "Localize in the code path that supplies this dynamic text.");
                    continue;
                }

                if (!item.IsExcluded)
                {
                    item.RecommendedApproach = LocalizationRecommendedApproaches.CodeApi;
                    item.Notes = AppendNote(
                        item.Notes,
                        "Code mutator hint found. Do not attach LocalizedText until the binding is reviewed.");
                }
            }
        }

        private static bool TryFindMutatorEvidence(
            LocalizationScanItem item,
            Dictionary<string, List<string>> mutatorsByToken,
            out string evidence)
        {
            evidence = string.Empty;
            var targetTokens = BuildTargetTokens(item);
            for (var i = 0; i < targetTokens.Count; i++)
            {
                if (mutatorsByToken.TryGetValue(targetTokens[i], out var hits))
                {
                    evidence = JoinEvidence(hits);
                    return true;
                }
            }

            return false;
        }

        private static List<string> BuildTargetTokens(LocalizationScanItem item)
        {
            var tokens = new List<string>();
            AddToken(tokens, item.TextComponentId);
            AddToken(tokens, LastHierarchySegment(item.HierarchyPath));
            return tokens;
        }

        private static void AddToken(List<string> tokens, string value)
        {
            var token = NormalizeNameForMatching(value);
            if (!string.IsNullOrEmpty(token) && !tokens.Contains(token))
            {
                tokens.Add(token);
            }
        }

        private static string LastHierarchySegment(string hierarchyPath)
        {
            if (string.IsNullOrEmpty(hierarchyPath))
            {
                return string.Empty;
            }

            var slash = hierarchyPath.LastIndexOf('/');
            return slash < 0 ? hierarchyPath : hierarchyPath.Substring(slash + 1);
        }

        private static string BuildTargetHint(LocalizationScanItem item)
        {
            return string.IsNullOrEmpty(item.TextComponentId) ? LastHierarchySegment(item.HierarchyPath) : item.TextComponentId;
        }

        private static string JoinEvidence(List<string> hits)
        {
            var builder = new System.Text.StringBuilder();
            for (var i = 0; i < hits.Count && i < 3; i++)
            {
                if (string.IsNullOrEmpty(hits[i]))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append("; ");
                }

                builder.Append(hits[i]);
            }

            if (hits.Count > 3)
            {
                builder.Append("; +").Append((hits.Count - 3).ToString()).Append(" more");
            }

            return builder.ToString();
        }

        private static string AppendNote(string notes, string extra)
        {
            return string.IsNullOrEmpty(notes) ? extra : notes + " " + extra;
        }

        internal static string NormalizeNameForMatching(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var bracket = value.IndexOf('[');
            if (bracket >= 0)
            {
                value = value.Substring(0, bracket);
            }

            var builder = new System.Text.StringBuilder(value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(char.ToLowerInvariant(c));
                }
            }

            var token = builder.ToString().TrimStart('_');
            var removedSuffix = true;
            while (removedSuffix)
            {
                removedSuffix = false;
                for (var i = 0; i < NameSuffixes.Length; i++)
                {
                    var suffix = NameSuffixes[i];
                    if (token.Length > suffix.Length && token.EndsWith(suffix, StringComparison.Ordinal))
                    {
                        token = token.Substring(0, token.Length - suffix.Length);
                        removedSuffix = true;
                        break;
                    }
                }
            }

            for (var i = 0; i < NameSuffixes.Length; i++)
            {
                if (string.Equals(token, NameSuffixes[i], StringComparison.Ordinal))
                {
                    return string.Empty;
                }
            }

            return token;
        }

        private static int CompareItems(LocalizationScanItem a, LocalizationScanItem b)
        {
            var c = string.CompareOrdinal(a.AssetPath, b.AssetPath);
            if (c != 0)
            {
                return c;
            }

            c = string.CompareOrdinal(a.HierarchyPath, b.HierarchyPath);
            if (c != 0)
            {
                return c;
            }

            c = string.CompareOrdinal(a.ComponentType, b.ComponentType);
            if (c != 0)
            {
                return c;
            }

            return string.CompareOrdinal(a.EnglishSource ?? string.Empty, b.EnglishSource ?? string.Empty);
        }
    }
}
