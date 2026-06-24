using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Loqui.Editor
{
    public sealed class LocalizationScanExportOptions
    {
        public bool IncludeMarkdown = true;
        public bool IncludeJsonInventory = true;
        public bool IncludeCsv = true;
        public bool IncludeAiBundle = true;
        public bool RefreshAssetDatabase = true;
    }

    public static class LocalizationScanExporter
    {
        public static List<string> ExportAll(
            IReadOnlyList<LocalizationScanItem> items,
            string outputFolder,
            string targetLanguage)
        {
            return Export(items, outputFolder, targetLanguage, new LocalizationScanExportOptions());
        }

        public static List<string> Export(
            IReadOnlyList<LocalizationScanItem> items,
            string outputFolder,
            string targetLanguage,
            LocalizationScanExportOptions options)
        {
            Directory.CreateDirectory(outputFolder);
            options ??= new LocalizationScanExportOptions();

            var written = new List<string>();
            if (options.IncludeMarkdown)
            {
                written.Add(WriteText(Path.Combine(outputFolder, "localization_scan.md"), BuildMarkdown(items)));
            }

            if (options.IncludeJsonInventory)
            {
                written.Add(WriteText(Path.Combine(outputFolder, "localization_scan.json"), BuildJson(items)));
            }

            if (options.IncludeCsv)
            {
                written.Add(WriteText(Path.Combine(outputFolder, "localization_scan.csv"), BuildCsv(items)));
            }

            if (options.IncludeAiBundle)
            {
                written.Add(WriteText(
                    Path.Combine(outputFolder, "localization_ai_bundle_" + targetLanguage + ".json"),
                    BuildAiBundle(items, targetLanguage)));
            }

            if (options.RefreshAssetDatabase)
            {
                AssetDatabase.Refresh();
            }

            return written;
        }

        public static string BuildMarkdown(IReadOnlyList<LocalizationScanItem> items)
        {
            var candidates = 0;
            var excluded = 0;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].IsCandidate)
                {
                    candidates++;
                }
                else
                {
                    excluded++;
                }
            }

            var builder = new StringBuilder();
            builder.AppendLine("# Localization Scan");
            builder.AppendLine();
            builder.AppendLine("Total: " + items.Count + " | Candidates: " + candidates + " | Excluded/advisory: " + excluded);
            builder.AppendLine();
            builder.AppendLine(
                "| Approach | Key | Group | Source | Component | Container | Hierarchy | Mutator Evidence | Review | Exclusion |");
            builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                builder.Append("| ").Append(Md(item.RecommendedApproach))
                    .Append(" | ").Append(Md(item.ProposedKey))
                    .Append(" | ").Append(Md(item.Group))
                    .Append(" | ").Append(Md(item.EnglishSource))
                    .Append(" | ").Append(Md(item.ComponentType))
                    .Append(" | ").Append(Md(item.ContainerName))
                    .Append(" | ").Append(Md(item.HierarchyPath))
                    .Append(" | ").Append(Md(item.MutationEvidence))
                    .Append(" | ").Append(item.RequiresReview ? "yes" : string.Empty)
                    .Append(" | ").Append(Md(item.ExclusionReason))
                    .AppendLine(" |");
            }

            return builder.ToString();
        }

        public static string BuildJson(IReadOnlyList<LocalizationScanItem> items)
        {
            var inventory = new LocalizationScanInventory { Count = items.Count };
            for (var i = 0; i < items.Count; i++)
            {
                inventory.Items.Add(items[i]);
                if (items[i].IsCandidate)
                {
                    inventory.CandidateCount++;
                }
            }

            return JsonUtility.ToJson(inventory, true);
        }

        public static string BuildCsv(IReadOnlyList<LocalizationScanItem> items)
        {
            var builder = new StringBuilder();
            builder.AppendLine(
                "RecommendedApproach,Key,Group,Source,ComponentType,ContainerKind,ContainerName,HierarchyPath," +
                "LineNumber,MaxLength,RequiresReview,CodeMutatorHint,MutationEvidence,ExclusionReason,Notes");
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                builder.Append(Csv(item.RecommendedApproach)).Append(',')
                    .Append(Csv(item.ProposedKey)).Append(',')
                    .Append(Csv(item.Group)).Append(',')
                    .Append(Csv(item.EnglishSource)).Append(',')
                    .Append(Csv(item.ComponentType)).Append(',')
                    .Append(Csv(item.ContainerKind)).Append(',')
                    .Append(Csv(item.ContainerName)).Append(',')
                    .Append(Csv(item.HierarchyPath)).Append(',')
                    .Append(item.LineNumber.ToString()).Append(',')
                    .Append(item.MaxLength.ToString()).Append(',')
                    .Append(item.RequiresReview ? "true" : "false").Append(',')
                    .Append(Csv(item.CodeMutatorHint)).Append(',')
                    .Append(Csv(item.MutationEvidence)).Append(',')
                    .Append(Csv(item.ExclusionReason)).Append(',')
                    .Append(Csv(item.Notes))
                    .Append('\n');
            }

            return builder.ToString();
        }

        public static string BuildAiBundle(IReadOnlyList<LocalizationScanItem> items, string targetLanguage)
        {
            var bundle = new LocalizationAiBundle { TargetLanguage = targetLanguage };
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (!item.IsCandidate || string.IsNullOrEmpty(item.ProposedKey))
                {
                    continue;
                }

                bundle.Entries.Add(new LocalizationAiBundleEntry
                {
                    Key = item.ProposedKey,
                    Group = item.Group,
                    Source = item.EnglishSource,
                    Context = item.Context,
                    MaxLength = item.MaxLength,
                    RecommendedApproach = item.RecommendedApproach,
                    MutationEvidence = item.MutationEvidence,
                    TargetLanguage = targetLanguage,
                    TargetDefault = string.Empty,
                    TargetIOS = string.Empty,
                    TargetAndroid = string.Empty
                });
            }

            bundle.Count = bundle.Entries.Count;
            return JsonUtility.ToJson(bundle, true);
        }

        private static string WriteText(string path, string content)
        {
            File.WriteAllText(path, content);
            return path;
        }

        private static string Md(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("|", "\\|").Replace("\n", " ").Replace("\r", " ");
        }

        private static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var escaped = value.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");
            return "\"" + escaped + "\"";
        }
    }
}
