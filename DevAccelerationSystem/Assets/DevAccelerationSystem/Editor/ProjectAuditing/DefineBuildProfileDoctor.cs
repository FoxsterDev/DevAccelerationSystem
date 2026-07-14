using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DevAccelerationSystem.ProjectAuditing
{
    [Serializable]
    public sealed class DefineDoctorOptions
    {
        public BuildTargetGroup[] TargetGroups = { BuildTargetGroup.Standalone, BuildTargetGroup.Android, BuildTargetGroup.iOS };
        public string[] RequiredSymbols = Array.Empty<string>();
        public string[] ForbiddenSymbols = Array.Empty<string>();
        public string[] RequiredBuildProfileNames = Array.Empty<string>();
        public bool DetectDrift = true;
    }

    [Serializable]
    public sealed class DefineSnapshot
    {
        public BuildTargetGroup TargetGroup;
        public string[] Symbols = Array.Empty<string>();
    }

    [Serializable]
    public sealed class DefineChange
    {
        public BuildTargetGroup TargetGroup;
        public string[] PreviousSymbols = Array.Empty<string>();
        public string[] ProposedSymbols = Array.Empty<string>();
    }

    [Serializable]
    public sealed class DefineChangeSet
    {
        public List<DefineChange> Changes = new List<DefineChange>();
    }

    [Serializable]
    public sealed class DefineBackup
    {
        public string SchemaVersion = AuditReport.CurrentSchemaVersion;
        public List<DefineChange> Entries = new List<DefineChange>();
    }

    public static class DefineBuildProfileDoctor
    {
        public static AuditReport AnalyzeProject(string projectRoot = null, DefineDoctorOptions options = null)
        {
            projectRoot ??= ProjectAuditPaths.GetProjectRoot();
            options ??= new DefineDoctorOptions();
            var snapshots = options.TargetGroups
                .Distinct()
                .OrderBy(group => group.ToString(), StringComparer.Ordinal)
                .Select(group => new DefineSnapshot
                {
                    TargetGroup = group,
                    Symbols = ReadSymbols(group)
                })
                .ToArray();
            return AnalyzeSnapshots(projectRoot, options, snapshots);
        }

        public static AuditReport AnalyzeSnapshots(string projectRoot, DefineDoctorOptions options, IReadOnlyList<DefineSnapshot> snapshots)
        {
            options ??= new DefineDoctorOptions();
            var report = new AuditReport
            {
                AuditName = "Define & Build Profile Doctor",
                ProjectRoot = projectRoot ?? string.Empty
            };
            var normalizedSnapshots = snapshots ?? Array.Empty<DefineSnapshot>();
            report.AddMetric("targetGroups", normalizedSnapshots.Count);
            foreach (var snapshot in normalizedSnapshots.OrderBy(value => value.TargetGroup.ToString(), StringComparer.Ordinal))
            {
                var symbols = NormalizeSymbols(snapshot.Symbols);
                foreach (var required in NormalizeSymbols(options.RequiredSymbols))
                {
                    if (!symbols.Contains(required, StringComparer.Ordinal))
                    {
                        report.Add(AuditSeverity.Error, "DASDEFINE001", $"Required symbol '{required}' is missing for {snapshot.TargetGroup}.", snapshot.TargetGroup.ToString(), "Preview the Define Doctor changes, apply them, then let Unity recompile before verification.");
                    }
                }

                foreach (var forbidden in NormalizeSymbols(options.ForbiddenSymbols))
                {
                    if (symbols.Contains(forbidden, StringComparer.Ordinal))
                    {
                        report.Add(AuditSeverity.Error, "DASDEFINE002", $"Forbidden symbol '{forbidden}' is present for {snapshot.TargetGroup}.", snapshot.TargetGroup.ToString(), "Preview the Define Doctor changes before removing the symbol.");
                    }
                }
            }

            if (options.DetectDrift)
            {
                AddDriftFindings(report, normalizedSnapshots);
            }

            AddAssemblyDefinitionFindings(projectRoot, normalizedSnapshots, report);
            AddBuildProfileFindings(projectRoot, options.RequiredBuildProfileNames, report);
            return report;
        }

        public static DefineChangeSet CreatePreview(DefineDoctorOptions options, IReadOnlyList<DefineSnapshot> snapshots)
        {
            options ??= new DefineDoctorOptions();
            var result = new DefineChangeSet();
            foreach (var snapshot in snapshots ?? Array.Empty<DefineSnapshot>())
            {
                var previous = NormalizeSymbols(snapshot.Symbols);
                var proposed = previous
                    .Concat(NormalizeSymbols(options.RequiredSymbols))
                    .Except(NormalizeSymbols(options.ForbiddenSymbols), StringComparer.Ordinal)
                    .OrderBy(symbol => symbol, StringComparer.Ordinal)
                    .ToArray();
                if (!previous.SequenceEqual(proposed, StringComparer.Ordinal))
                {
                    result.Changes.Add(new DefineChange
                    {
                        TargetGroup = snapshot.TargetGroup,
                        PreviousSymbols = previous,
                        ProposedSymbols = proposed
                    });
                }
            }

            return result;
        }

        public static string Apply(DefineChangeSet changeSet, string backupPath)
        {
            if (changeSet == null)
            {
                throw new ArgumentNullException(nameof(changeSet));
            }

            if (string.IsNullOrWhiteSpace(backupPath))
            {
                throw new ArgumentException("A backup path is required.", nameof(backupPath));
            }

            var backup = new DefineBackup
            {
                Entries = changeSet.Changes.Select(change => new DefineChange
                {
                    TargetGroup = change.TargetGroup,
                    PreviousSymbols = ReadSymbols(change.TargetGroup),
                    ProposedSymbols = change.ProposedSymbols
                }).ToList()
            };
            var directory = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(backupPath, JsonUtility.ToJson(backup, true));
            foreach (var change in changeSet.Changes)
            {
                WriteSymbols(change.TargetGroup, change.ProposedSymbols);
            }

            return backupPath;
        }

        public static void Restore(string backupPath)
        {
            if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath))
            {
                throw new FileNotFoundException("Define Doctor backup was not found.", backupPath);
            }

            var backup = JsonUtility.FromJson<DefineBackup>(File.ReadAllText(backupPath));
            if (backup?.Entries == null)
            {
                throw new InvalidDataException("Define Doctor backup is invalid.");
            }

            foreach (var entry in backup.Entries)
            {
                WriteSymbols(entry.TargetGroup, entry.PreviousSymbols);
            }
        }

        public static DefineSnapshot[] CaptureSnapshots(IEnumerable<BuildTargetGroup> targetGroups)
        {
            return (targetGroups ?? Array.Empty<BuildTargetGroup>())
                .Distinct()
                .OrderBy(group => group.ToString(), StringComparer.Ordinal)
                .Select(group => new DefineSnapshot
                {
                    TargetGroup = group,
                    Symbols = ReadSymbols(group)
                })
                .ToArray();
        }

        private static string[] ReadSymbols(BuildTargetGroup targetGroup)
        {
#if UNITY_2021_2_OR_NEWER
            return NormalizeSymbols(new[] { PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup)) });
#else
            return NormalizeSymbols(new[] { PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup) });
#endif
        }

        private static void WriteSymbols(BuildTargetGroup targetGroup, IEnumerable<string> symbols)
        {
            var value = string.Join(";", NormalizeSymbols(symbols));
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup), value);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, value);
#endif
        }

        private static void AddDriftFindings(AuditReport report, IReadOnlyList<DefineSnapshot> snapshots)
        {
            if (snapshots.Count < 2)
            {
                return;
            }

            var allSymbols = snapshots.SelectMany(snapshot => NormalizeSymbols(snapshot.Symbols)).Distinct(StringComparer.Ordinal).OrderBy(symbol => symbol, StringComparer.Ordinal);
            foreach (var symbol in allSymbols)
            {
                var definedGroups = snapshots
                    .Where(snapshot => NormalizeSymbols(snapshot.Symbols).Contains(symbol, StringComparer.Ordinal))
                    .Select(snapshot => snapshot.TargetGroup.ToString())
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToArray();
                if (definedGroups.Length > 0 && definedGroups.Length < snapshots.Count)
                {
                    report.Add(AuditSeverity.Warning, "DASDEFINE003", $"Symbol '{symbol}' drifts across target groups: {string.Join(", ", definedGroups)} define it.", "PlayerSettings", "Confirm whether the target-specific difference is intentional or preview a synchronized change.");
                }
            }
        }

        private static void AddAssemblyDefinitionFindings(string projectRoot, IReadOnlyList<DefineSnapshot> snapshots, AuditReport report)
        {
            if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
            {
                return;
            }

            var definedSymbols = snapshots.SelectMany(snapshot => NormalizeSymbols(snapshot.Symbols)).ToHashSet(StringComparer.Ordinal);
            foreach (var asmdefPath in Directory.GetFiles(projectRoot, "*.asmdef", SearchOption.AllDirectories).Where(path => !path.Replace('\\', '/').Contains("/Library/")))
            {
                var relativePath = ProjectAuditPaths.ToProjectRelativePath(projectRoot, asmdefPath);
                AssemblyDefinitionDocument document;
                try
                {
                    document = JsonUtility.FromJson<AssemblyDefinitionDocument>(File.ReadAllText(asmdefPath));
                }
                catch (ArgumentException)
                {
                    report.Add(AuditSeverity.Error, "DASDEFINE007", "Assembly definition is invalid JSON.", relativePath, "Fix the asmdef JSON before validating define constraints.");
                    continue;
                }

                foreach (var constraint in document?.defineConstraints ?? Array.Empty<string>())
                {
                    if (!string.IsNullOrWhiteSpace(constraint) && !definedSymbols.Contains(constraint))
                    {
                        report.Add(AuditSeverity.Warning, "DASDEFINE004", $"Assembly define constraint '{constraint}' is not defined by the inspected target groups.", relativePath, "Confirm the assembly is intentionally excluded or add the required symbol through an approved preview.");
                    }
                }

                foreach (var versionDefine in document?.versionDefines ?? Array.Empty<VersionDefineDocument>())
                {
                    if (string.IsNullOrWhiteSpace(versionDefine.name) || string.IsNullOrWhiteSpace(versionDefine.expression) || string.IsNullOrWhiteSpace(versionDefine.define))
                    {
                        report.Add(AuditSeverity.Error, "DASDEFINE005", "Assembly versionDefine requires name, expression, and define.", relativePath, "Fix the asmdef versionDefine entry.");
                    }
                }
            }
        }

        private static void AddBuildProfileFindings(string projectRoot, IEnumerable<string> requiredProfileNames, AuditReport report)
        {
            var profiles = DiscoverBuildProfileNames(projectRoot);
            report.AddMetric("buildProfilesDiscovered", profiles.Length);
            foreach (var requiredProfileName in (requiredProfileNames ?? Array.Empty<string>()).Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => name.Trim()).Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal))
            {
                if (!profiles.Contains(requiredProfileName, StringComparer.Ordinal))
                {
                    report.Add(AuditSeverity.Error, "DASDEFINE006", $"Required Build Profile '{requiredProfileName}' was not found.", "BuildProfiles", "Create or restore the required Build Profile, then run the read-only audit again.");
                }
            }
        }

        private static string[] DiscoverBuildProfileNames(string projectRoot)
        {
            var profiles = new HashSet<string>(StringComparer.Ordinal);
            if (string.Equals(Path.GetFullPath(projectRoot ?? string.Empty), Path.GetFullPath(ProjectAuditPaths.GetProjectRoot()), StringComparison.OrdinalIgnoreCase))
            {
                foreach (var guid in AssetDatabase.FindAssets("t:BuildProfile"))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        profiles.Add(Path.GetFileNameWithoutExtension(path));
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(projectRoot) && Directory.Exists(projectRoot))
            {
                foreach (var path in Directory.GetFiles(projectRoot, "*BuildProfile*.asset", SearchOption.AllDirectories).Where(path => !path.Replace('\\', '/').Contains("/Library/")))
                {
                    profiles.Add(Path.GetFileNameWithoutExtension(path));
                }
            }

            return profiles.OrderBy(name => name, StringComparer.Ordinal).ToArray();
        }

        private static string[] NormalizeSymbols(IEnumerable<string> symbols)
        {
            if (symbols == null)
            {
                return Array.Empty<string>();
            }

            return symbols
                .SelectMany(symbol => (symbol ?? string.Empty).Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(symbol => symbol.Trim())
                .Where(symbol => !string.IsNullOrEmpty(symbol))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(symbol => symbol, StringComparer.Ordinal)
                .ToArray();
        }

        [Serializable]
        private sealed class AssemblyDefinitionDocument
        {
            public string[] defineConstraints;
            public VersionDefineDocument[] versionDefines;
        }

        [Serializable]
        private sealed class VersionDefineDocument
        {
            public string name;
            public string expression;
            public string define;
        }
    }
}
