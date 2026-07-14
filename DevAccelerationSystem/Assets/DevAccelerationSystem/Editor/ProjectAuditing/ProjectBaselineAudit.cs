using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace DevAccelerationSystem.ProjectAuditing
{
    [Serializable]
    public sealed class BaselinePolicy
    {
        public string SchemaVersion = AuditReport.CurrentSchemaVersion;
        public string RequiredColorSpace;
        public BaselinePackage[] RequiredPackages = Array.Empty<BaselinePackage>();
        public string[] ForbiddenPackageIds = Array.Empty<string>();
        public string[] RequiredProjectFiles = Array.Empty<string>();
        public BaselineTargetPolicy[] Targets = Array.Empty<BaselineTargetPolicy>();
    }

    [Serializable]
    public sealed class BaselinePackage
    {
        public string Name;
        public string Version;
    }

    [Serializable]
    public sealed class BaselineTargetPolicy
    {
        public string TargetGroup;
        public string ScriptingBackend;
        public string[] RequiredSymbols = Array.Empty<string>();
    }

    [Serializable]
    public sealed class BaselineTargetSnapshot
    {
        public BuildTargetGroup TargetGroup;
        public string ScriptingBackend;
        public string[] Symbols = Array.Empty<string>();
    }

    [Serializable]
    public sealed class BaselineContext
    {
        public string ProjectRoot;
        public string ColorSpace;
        public BaselinePackage[] Packages = Array.Empty<BaselinePackage>();
        public string[] ExistingProjectFiles = Array.Empty<string>();
        public BaselineTargetSnapshot[] Targets = Array.Empty<BaselineTargetSnapshot>();
    }

    [Serializable]
    public sealed class BaselineRemediationPreview
    {
        public bool NeedsColorSpaceUpdate;
        public string CurrentColorSpace;
        public string ProposedColorSpace;
        public DefineChangeSet DefineChanges = new DefineChangeSet();
    }

    [Serializable]
    public sealed class BaselineBackup
    {
        public string SchemaVersion = AuditReport.CurrentSchemaVersion;
        public string ColorSpace;
        public string DefineBackupPath;
    }

    public static class ProjectBaselineAudit
    {
        public static AuditReport AnalyzeProject(string baselinePath, string projectRoot = null)
        {
            projectRoot ??= ProjectAuditPaths.GetProjectRoot();
            if (string.IsNullOrWhiteSpace(baselinePath) || !File.Exists(baselinePath))
            {
                var missingReport = new AuditReport
                {
                    AuditName = "Project Baseline Audit",
                    ProjectRoot = projectRoot
                };
                missingReport.Add(AuditSeverity.Error, "DASBASE001", "Baseline policy file was not found.", baselinePath ?? string.Empty, "Add a version-controlled baseline JSON file and pass its project-relative path.");
                return missingReport;
            }

            try
            {
                return Evaluate(ReadPolicy(File.ReadAllText(baselinePath)), CaptureContext(projectRoot));
            }
            catch (ArgumentException)
            {
                var invalidReport = new AuditReport
                {
                    AuditName = "Project Baseline Audit",
                    ProjectRoot = projectRoot
                };
                invalidReport.Add(AuditSeverity.Error, "DASBASE002", "Baseline policy is invalid JSON.", baselinePath, "Fix the baseline JSON using the documented schema.");
                return invalidReport;
            }
        }

        public static AuditReport Evaluate(BaselinePolicy policy, BaselineContext context)
        {
            var report = new AuditReport
            {
                AuditName = "Project Baseline Audit",
                ProjectRoot = context?.ProjectRoot ?? string.Empty
            };
            if (policy == null)
            {
                report.Add(AuditSeverity.Error, "DASBASE002", "Baseline policy is invalid.", string.Empty, "Use the documented schema version and fields.");
                return report;
            }

            if (!string.Equals(policy.SchemaVersion, AuditReport.CurrentSchemaVersion, StringComparison.Ordinal))
            {
                report.Add(AuditSeverity.Error, "DASBASE003", $"Baseline schema '{policy.SchemaVersion}' is unsupported.", string.Empty, $"Use schemaVersion '{AuditReport.CurrentSchemaVersion}'.");
            }

            context ??= new BaselineContext();
            var packages = (context.Packages ?? Array.Empty<BaselinePackage>())
                .Where(package => !string.IsNullOrWhiteSpace(package?.Name))
                .GroupBy(package => package.Name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().Version ?? string.Empty, StringComparer.Ordinal);
            report.AddMetric("packagesDiscovered", packages.Count);
            ValidatePackages(policy, packages, report);
            ValidateFiles(policy, context, report);
            ValidateColorSpace(policy, context, report);
            ValidateTargets(policy, context, report);
            return report;
        }

        public static BaselineRemediationPreview CreatePreview(BaselinePolicy policy, BaselineContext context)
        {
            policy ??= new BaselinePolicy();
            context ??= new BaselineContext();
            var preview = new BaselineRemediationPreview
            {
                CurrentColorSpace = context.ColorSpace ?? string.Empty,
                ProposedColorSpace = policy.RequiredColorSpace ?? string.Empty,
                NeedsColorSpaceUpdate = !string.IsNullOrWhiteSpace(policy.RequiredColorSpace) && !string.Equals(context.ColorSpace, policy.RequiredColorSpace, StringComparison.Ordinal)
            };
            var snapshotsByTarget = (context.Targets ?? Array.Empty<BaselineTargetSnapshot>())
                .ToDictionary(target => target.TargetGroup);
            foreach (var targetPolicy in policy.Targets ?? Array.Empty<BaselineTargetPolicy>())
            {
                if (!TryParseTargetGroup(targetPolicy.TargetGroup, out var targetGroup) || !snapshotsByTarget.TryGetValue(targetGroup, out var snapshot))
                {
                    continue;
                }

                var targetPreview = DefineBuildProfileDoctor.CreatePreview(new DefineDoctorOptions
                {
                    RequiredSymbols = targetPolicy.RequiredSymbols ?? Array.Empty<string>()
                }, new[]
                {
                    new DefineSnapshot
                    {
                        TargetGroup = snapshot.TargetGroup,
                        Symbols = snapshot.Symbols
                    }
                });
                preview.DefineChanges.Changes.AddRange(targetPreview.Changes);
            }

            return preview;
        }

        public static string Apply(BaselineRemediationPreview preview, string backupPath, bool applyColorSpace, bool applyDefines)
        {
            if (preview == null)
            {
                throw new ArgumentNullException(nameof(preview));
            }

            if (string.IsNullOrWhiteSpace(backupPath))
            {
                throw new ArgumentException("A backup path is required.", nameof(backupPath));
            }

            var directory = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var shouldApplyColorSpace = applyColorSpace && preview.NeedsColorSpaceUpdate;
            var parsedColorSpace = default(ColorSpace);
            if (shouldApplyColorSpace && !Enum.TryParse(preview.ProposedColorSpace, out parsedColorSpace))
            {
                throw new ArgumentException("The proposed color space is invalid.", nameof(preview));
            }

            var backup = new BaselineBackup
            {
                ColorSpace = PlayerSettings.colorSpace.ToString()
            };
            if (applyDefines && preview.DefineChanges?.Changes?.Count > 0)
            {
                backup.DefineBackupPath = Path.ChangeExtension(backupPath, ".defines.json");
            }

            File.WriteAllText(backupPath, JsonUtility.ToJson(backup, true));
            if (applyDefines && preview.DefineChanges?.Changes?.Count > 0)
            {
                DefineBuildProfileDoctor.Apply(preview.DefineChanges, backup.DefineBackupPath);
            }

            if (shouldApplyColorSpace)
            {
                PlayerSettings.colorSpace = parsedColorSpace;
            }

            return backupPath;
        }

        public static void Restore(string backupPath)
        {
            if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath))
            {
                throw new FileNotFoundException("Baseline backup was not found.", backupPath);
            }

            var backup = JsonUtility.FromJson<BaselineBackup>(File.ReadAllText(backupPath));
            if (backup == null || !Enum.TryParse(backup.ColorSpace, out ColorSpace colorSpace))
            {
                throw new InvalidDataException("Baseline backup is invalid.");
            }

            if (!string.IsNullOrWhiteSpace(backup.DefineBackupPath))
            {
                DefineBuildProfileDoctor.Restore(backup.DefineBackupPath);
            }

            PlayerSettings.colorSpace = colorSpace;
        }

        public static BaselineContext CaptureContext(string projectRoot = null)
        {
            projectRoot ??= ProjectAuditPaths.GetProjectRoot();
            var packageInfos = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages() ?? Array.Empty<UnityEditor.PackageManager.PackageInfo>();
            var targetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android, BuildTargetGroup.iOS };
            return new BaselineContext
            {
                ProjectRoot = projectRoot,
                ColorSpace = PlayerSettings.colorSpace.ToString(),
                Packages = packageInfos.Select(info => new BaselinePackage
                {
                    Name = info.name,
                    Version = info.version
                }).ToArray(),
                ExistingProjectFiles = Array.Empty<string>(),
                Targets = targetGroups.Select(group => new BaselineTargetSnapshot
                {
                    TargetGroup = group,
                    ScriptingBackend = GetScriptingBackend(group),
                    Symbols = DefineBuildProfileDoctor.CaptureSnapshots(new[] { group }).First().Symbols
                }).ToArray()
            };
        }

        private static void ValidatePackages(BaselinePolicy policy, IReadOnlyDictionary<string, string> packages, AuditReport report)
        {
            foreach (var required in policy.RequiredPackages ?? Array.Empty<BaselinePackage>())
            {
                if (string.IsNullOrWhiteSpace(required?.Name))
                {
                    report.Add(AuditSeverity.Error, "DASBASE004", "A required package entry has no name.", "requiredPackages", "Set a package name in the baseline policy.");
                    continue;
                }

                if (!packages.TryGetValue(required.Name, out var installedVersion))
                {
                    report.Add(AuditSeverity.Error, "DASBASE005", $"Required package '{required.Name}' is not installed.", "Packages/manifest.json", "Add the required package through Unity Package Manager.");
                }
                else if (!string.IsNullOrWhiteSpace(required.Version) && !string.Equals(required.Version, installedVersion, StringComparison.Ordinal))
                {
                    report.Add(AuditSeverity.Error, "DASBASE006", $"Required package '{required.Name}' has version '{installedVersion}' instead of '{required.Version}'.", "Packages/packages-lock.json", "Use the policy-approved package version.");
                }
            }

            foreach (var forbidden in policy.ForbiddenPackageIds ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(forbidden) && packages.ContainsKey(forbidden))
                {
                    report.Add(AuditSeverity.Error, "DASBASE007", $"Forbidden package '{forbidden}' is installed.", "Packages/manifest.json", "Remove the package through Unity Package Manager after confirming no consumer requires it.");
                }
            }
        }

        private static void ValidateFiles(BaselinePolicy policy, BaselineContext context, AuditReport report)
        {
            var files = new HashSet<string>(context.ExistingProjectFiles ?? Array.Empty<string>(), StringComparer.Ordinal);
            foreach (var requiredFile in policy.RequiredProjectFiles ?? Array.Empty<string>())
            {
                var fullPath = string.IsNullOrWhiteSpace(context.ProjectRoot) || string.IsNullOrWhiteSpace(requiredFile)
                    ? string.Empty
                    : Path.GetFullPath(Path.Combine(context.ProjectRoot, requiredFile));
                if (string.IsNullOrWhiteSpace(requiredFile) || Path.IsPathRooted(requiredFile) || string.IsNullOrWhiteSpace(context.ProjectRoot) || !ProjectAuditPaths.IsInside(context.ProjectRoot, fullPath))
                {
                    report.Add(AuditSeverity.Error, "DASBASE008", "Required project file must be a non-empty project-relative path.", "requiredProjectFiles", "Use a project-relative path such as Assets/Configuration.asset.");
                }
                else if (!files.Contains(requiredFile.Replace('\\', '/')) && !File.Exists(fullPath))
                {
                    report.Add(AuditSeverity.Error, "DASBASE009", $"Required project file '{requiredFile}' is missing.", requiredFile, "Add the required version-controlled project file.");
                }
            }
        }

        private static void ValidateColorSpace(BaselinePolicy policy, BaselineContext context, AuditReport report)
        {
            if (string.IsNullOrWhiteSpace(policy.RequiredColorSpace))
            {
                return;
            }

            if (!Enum.TryParse(policy.RequiredColorSpace, out ColorSpace _))
            {
                report.Add(AuditSeverity.Error, "DASBASE014", $"Required color space '{policy.RequiredColorSpace}' is invalid.", "requiredColorSpace", "Use a valid Unity ColorSpace name such as Gamma or Linear.");
            }
            else if (!string.Equals(policy.RequiredColorSpace, context.ColorSpace, StringComparison.Ordinal))
            {
                report.Add(AuditSeverity.Error, "DASBASE010", $"Color space is '{context.ColorSpace}' instead of required '{policy.RequiredColorSpace}'.", "ProjectSettings", "Preview the baseline remediation before changing PlayerSettings.");
            }
        }

        private static void ValidateTargets(BaselinePolicy policy, BaselineContext context, AuditReport report)
        {
            var snapshots = (context.Targets ?? Array.Empty<BaselineTargetSnapshot>()).ToDictionary(snapshot => snapshot.TargetGroup);
            foreach (var targetPolicy in policy.Targets ?? Array.Empty<BaselineTargetPolicy>())
            {
                if (!TryParseTargetGroup(targetPolicy.TargetGroup, out var targetGroup))
                {
                    report.Add(AuditSeverity.Error, "DASBASE011", $"Baseline target '{targetPolicy.TargetGroup}' is not a valid BuildTargetGroup.", "targets", "Use a BuildTargetGroup name such as Android or Standalone.");
                    continue;
                }

                if (!snapshots.TryGetValue(targetGroup, out var snapshot))
                {
                    report.Add(AuditSeverity.Error, "DASBASE011", $"Baseline target '{targetGroup}' was not inspected.", "targets", "Include the target group in the audit context.");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(targetPolicy.ScriptingBackend) && !string.Equals(targetPolicy.ScriptingBackend, snapshot.ScriptingBackend, StringComparison.Ordinal))
                {
                    report.Add(AuditSeverity.Error, "DASBASE012", $"Scripting backend for {targetGroup} is '{snapshot.ScriptingBackend}' instead of '{targetPolicy.ScriptingBackend}'.", targetGroup.ToString(), "Update the PlayerSettings backend through an approved project policy change.");
                }

                var symbols = new HashSet<string>(snapshot.Symbols ?? Array.Empty<string>(), StringComparer.Ordinal);
                foreach (var requiredSymbol in targetPolicy.RequiredSymbols ?? Array.Empty<string>())
                {
                    if (!string.IsNullOrWhiteSpace(requiredSymbol) && !symbols.Contains(requiredSymbol))
                    {
                        report.Add(AuditSeverity.Error, "DASBASE013", $"Required symbol '{requiredSymbol}' is missing for {targetGroup}.", targetGroup.ToString(), "Create a Define Doctor preview, apply it explicitly, then let Unity recompile.");
                    }
                }
            }
        }

        private static string GetScriptingBackend(BuildTargetGroup group)
        {
            try
            {
                return PlayerSettings.GetScriptingBackend(group).ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool TryParseTargetGroup(string value, out BuildTargetGroup targetGroup)
        {
            return Enum.TryParse(value, true, out targetGroup) && targetGroup != BuildTargetGroup.Unknown;
        }

        private static BaselinePolicy ReadPolicy(string json)
        {
            var document = JsonUtility.FromJson<BaselinePolicyDocument>(json);
            if (document == null)
            {
                return null;
            }

            return new BaselinePolicy
            {
                SchemaVersion = document.schemaVersion,
                RequiredColorSpace = document.requiredColorSpace,
                RequiredPackages = (document.requiredPackages ?? Array.Empty<BaselinePackageDocument>()).Select(package => new BaselinePackage
                {
                    Name = package.name,
                    Version = package.version
                }).ToArray(),
                ForbiddenPackageIds = document.forbiddenPackageIds ?? Array.Empty<string>(),
                RequiredProjectFiles = document.requiredProjectFiles ?? Array.Empty<string>(),
                Targets = (document.targets ?? Array.Empty<BaselineTargetPolicyDocument>()).Select(target => new BaselineTargetPolicy
                {
                    TargetGroup = target.targetGroup,
                    ScriptingBackend = target.scriptingBackend,
                    RequiredSymbols = target.requiredSymbols ?? Array.Empty<string>()
                }).ToArray()
            };
        }

        [Serializable]
        private sealed class BaselinePolicyDocument
        {
            public string schemaVersion;
            public string requiredColorSpace;
            public BaselinePackageDocument[] requiredPackages;
            public string[] forbiddenPackageIds;
            public string[] requiredProjectFiles;
            public BaselineTargetPolicyDocument[] targets;
        }

        [Serializable]
        private sealed class BaselinePackageDocument
        {
            public string name;
            public string version;
        }

        [Serializable]
        private sealed class BaselineTargetPolicyDocument
        {
            public string targetGroup;
            public string scriptingBackend;
            public string[] requiredSymbols;
        }
    }
}
