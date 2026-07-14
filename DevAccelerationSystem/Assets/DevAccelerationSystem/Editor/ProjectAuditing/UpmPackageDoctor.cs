using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DevAccelerationSystem.ProjectAuditing
{
    public sealed class PackageDoctorOptions
    {
        public AuditSeverity MinimumSeverity = AuditSeverity.Recommendation;
        public string[] KnownTags;
    }

    public static class UpmPackageDoctor
    {
        private static readonly Regex PackageIdPattern = new Regex("^[a-z0-9][a-z0-9.-]*\\.[a-z0-9][a-z0-9.-]*\\.[a-z0-9][a-z0-9.-]*$", RegexOptions.Compiled);
        private static readonly Regex SemVerPattern = new Regex("^(0|[1-9]\\d*)\\.(0|[1-9]\\d*)\\.(0|[1-9]\\d*)(?:-[0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*)?(?:\\+[0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*)?$", RegexOptions.Compiled);
        private static readonly Regex DependenciesObjectPattern = new Regex("\\\"dependencies\\\"\\s*:\\s*\\{", RegexOptions.Compiled);
        private static readonly Regex EmptyDependencyPattern = new Regex("\\\"[^\\\"]+\\\"\\s*:\\s*\\\"\\s*\\\"", RegexOptions.Compiled);
        private static readonly Regex AbsolutePathPattern = new Regex("(?<![A-Za-z0-9_])(?:" + string.Concat("/", "Users/") + "|[A-Za-z]:\\\\)", RegexOptions.Compiled);

        public static AuditReport AnalyzeProject(string projectRoot = null, PackageDoctorOptions options = null)
        {
            projectRoot ??= ProjectAuditPaths.GetProjectRoot();
            options ??= new PackageDoctorOptions();
            var report = new AuditReport
            {
                AuditName = "UPM Package Doctor",
                ProjectRoot = projectRoot
            };
            var manifestPaths = DiscoverPackageManifests(projectRoot);
            var tags = options.KnownTags ?? ReadLocalTags(projectRoot);
            report.AddMetric("packagesDiscovered", manifestPaths.Count);
            report.AddMetric("knownTags", tags.Length);
            foreach (var manifestPath in manifestPaths)
            {
                AnalyzeManifest(projectRoot, manifestPath, tags, report);
            }

            report.Findings = report.Findings.Where(finding => finding.Severity <= options.MinimumSeverity).ToList();
            return report;
        }

        private static List<string> DiscoverPackageManifests(string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
            {
                return new List<string>();
            }

            var projectManifest = Path.Combine(projectRoot, "Packages", "manifest.json");
            return Directory.GetFiles(projectRoot, "package.json", SearchOption.AllDirectories)
                .Where(path => !IsIgnoredPath(path))
                .Where(path => !string.Equals(path, projectManifest, StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
        }

        private static bool IsIgnoredPath(string path)
        {
            var normalized = path.Replace('\\', '/');
            return normalized.Contains("/Library/") || normalized.Contains("/Temp/") || normalized.Contains("/.git/") || normalized.Contains("/obj/");
        }

        private static void AnalyzeManifest(string projectRoot, string manifestPath, string[] tags, AuditReport report)
        {
            var relativeManifestPath = ProjectAuditPaths.ToProjectRelativePath(projectRoot, manifestPath);
            string rawManifest;
            try
            {
                rawManifest = File.ReadAllText(manifestPath);
            }
            catch (IOException)
            {
                report.Add(AuditSeverity.Error, "DASUPM001", "Package manifest could not be read.", relativeManifestPath, "Restore read access to package.json before publishing.");
                return;
            }
            ManifestDocument manifest;
            try
            {
                manifest = JsonUtility.FromJson<ManifestDocument>(rawManifest);
            }
            catch (ArgumentException exception)
            {
                report.Add(AuditSeverity.Error, "DASUPM001", $"Package manifest is invalid JSON: {exception.Message}", relativeManifestPath, "Fix package.json before publishing.");
                return;
            }

            if (manifest == null)
            {
                report.Add(AuditSeverity.Error, "DASUPM001", "Package manifest is invalid JSON.", relativeManifestPath, "Fix package.json before publishing.");
                return;
            }

            var packageRoot = Path.GetDirectoryName(manifestPath);
            ValidateIdentity(manifest, relativeManifestPath, report);
            ValidatePackageRoot(projectRoot, packageRoot, report);
            RequireFile(projectRoot, packageRoot, "README.md", "DASUPM005", report);
            RequireFile(projectRoot, packageRoot, "CHANGELOG.md", "DASUPM006", report);
            RequireFile(projectRoot, packageRoot, "LICENSE.md", "DASUPM007", report);
            ValidateUrl(manifest.documentationUrl, "documentationUrl", "DASUPM008", relativeManifestPath, report);
            ValidateUrl(manifest.changelogUrl, "changelogUrl", "DASUPM009", relativeManifestPath, report);
            ValidateUrl(manifest.licensesUrl, "licensesUrl", "DASUPM010", relativeManifestPath, report);
            ValidateRepositoryUrl(manifest.repository?.url, relativeManifestPath, report);
            ValidateDependencies(rawManifest, relativeManifestPath, report);
            ValidateTests(projectRoot, packageRoot, manifest, report);
            ValidateSamples(projectRoot, packageRoot, report);
            ValidateAbsolutePaths(projectRoot, packageRoot, report);
            ValidateTag(manifest.name, manifest.version, tags, relativeManifestPath, report);
        }

        private static void ValidateIdentity(ManifestDocument manifest, string relativeManifestPath, AuditReport report)
        {
            if (string.IsNullOrWhiteSpace(manifest.name) || !PackageIdPattern.IsMatch(manifest.name))
            {
                report.Add(AuditSeverity.Error, "DASUPM002", "Package id is missing or is not reverse-domain notation.", relativeManifestPath, "Use a lowercase reverse-domain package name.");
            }

            if (string.IsNullOrWhiteSpace(manifest.version) || !SemVerPattern.IsMatch(manifest.version))
            {
                report.Add(AuditSeverity.Error, "DASUPM003", "Package version is missing or is not semantic versioning.", relativeManifestPath, "Set package.json version to a valid SemVer value.");
            }

            if (string.IsNullOrWhiteSpace(manifest.displayName) || string.IsNullOrWhiteSpace(manifest.description) || string.IsNullOrWhiteSpace(manifest.unity))
            {
                report.Add(AuditSeverity.Error, "DASUPM004", "Package displayName, description, and unity metadata are required.", relativeManifestPath, "Complete the public package metadata.");
            }
        }

        private static void ValidatePackageRoot(string projectRoot, string packageRoot, AuditReport report)
        {
            var assetsDirectory = Path.Combine(projectRoot, "Assets");
            var packagesDirectory = Path.Combine(projectRoot, "Packages");
            if (!ProjectAuditPaths.IsInside(assetsDirectory, packageRoot) && !ProjectAuditPaths.IsInside(packagesDirectory, packageRoot))
            {
                report.Add(AuditSeverity.Warning, "DASUPM012", "Package root is outside Assets or Packages.", ProjectAuditPaths.ToProjectRelativePath(projectRoot, packageRoot), "Keep the Git UPM path stable and document it.");
            }
        }

        private static void RequireFile(string projectRoot, string packageRoot, string fileName, string code, AuditReport report)
        {
            if (!File.Exists(Path.Combine(packageRoot, fileName)))
            {
                report.Add(AuditSeverity.Error, code, $"Package is missing {fileName}.", ProjectAuditPaths.ToProjectRelativePath(projectRoot, packageRoot), $"Add {fileName} to the package root.");
            }
        }

        private static void ValidateUrl(string url, string fieldName, string code, string relativeManifestPath, AuditReport report)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                report.Add(AuditSeverity.Error, code, $"{fieldName} must be an absolute HTTP(S) URL.", relativeManifestPath, "Use the public URL for this package resource.");
            }
        }

        private static void ValidateDependencies(string rawManifest, string relativeManifestPath, AuditReport report)
        {
            if (!DependenciesObjectPattern.IsMatch(rawManifest))
            {
                report.Add(AuditSeverity.Error, "DASUPM013", "Package dependencies must be a JSON object.", relativeManifestPath, "Declare an empty dependencies object when the package has no dependencies.");
            }
            else if (EmptyDependencyPattern.IsMatch(rawManifest))
            {
                report.Add(AuditSeverity.Error, "DASUPM014", "Package dependencies contain an empty version.", relativeManifestPath, "Declare a non-empty dependency version.");
            }
        }

        private static void ValidateRepositoryUrl(string url, string relativeManifestPath, AuditReport report)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !uri.AbsolutePath.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            {
                report.Add(AuditSeverity.Error, "DASUPM011", "repository.url must be an absolute HTTPS Git URL ending in .git.", relativeManifestPath, "Use the public Git repository URL required by Git UPM.");
            }
        }

        private static void ValidateTests(string projectRoot, string packageRoot, ManifestDocument manifest, AuditReport report)
        {
            var testsDirectory = Path.Combine(packageRoot, "Tests");
            if (!Directory.Exists(testsDirectory) || !Directory.GetFiles(testsDirectory, "*.asmdef", SearchOption.AllDirectories).Any())
            {
                report.Add(AuditSeverity.Warning, "DASUPM015", "Package does not expose a test assembly under Tests.", ProjectAuditPaths.ToProjectRelativePath(projectRoot, packageRoot), "Add focused package tests and declare them in package.json testables.");
            }

            if (manifest.testables == null || manifest.testables.Length == 0 || manifest.testables.Any(string.IsNullOrWhiteSpace))
            {
                report.Add(AuditSeverity.Warning, "DASUPM020", "Package testables metadata is missing or incomplete.", ProjectAuditPaths.ToProjectRelativePath(projectRoot, packageRoot), "Declare each public test assembly in package.json testables.");
            }
        }

        private static void ValidateSamples(string projectRoot, string packageRoot, AuditReport report)
        {
            if (!Directory.Exists(Path.Combine(packageRoot, "Samples~")) && !Directory.Exists(Path.Combine(packageRoot, "Examples")))
            {
                report.Add(AuditSeverity.Recommendation, "DASUPM016", "Package has no Samples~ or Examples directory.", ProjectAuditPaths.ToProjectRelativePath(projectRoot, packageRoot), "Add a minimal sample when public integration requires one.");
            }
        }

        private static void ValidateAbsolutePaths(string projectRoot, string packageRoot, AuditReport report)
        {
            foreach (var path in Directory.GetFiles(packageRoot, "*.*", SearchOption.AllDirectories)
                         .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".asmref", StringComparison.OrdinalIgnoreCase))
                         .Where(path => !path.Replace('\\', '/').Contains("/Tests/")))
            {
                if (AbsolutePathPattern.IsMatch(File.ReadAllText(path)))
                {
                    report.Add(AuditSeverity.Error, "DASUPM017", "Package source contains an absolute developer path.", ProjectAuditPaths.ToProjectRelativePath(projectRoot, path), "Replace the path with a project-relative or configurable value.");
                }
            }
        }

        private static void ValidateTag(string packageId, string version, string[] tags, string relativeManifestPath, AuditReport report)
        {
            if (string.IsNullOrWhiteSpace(packageId) || string.IsNullOrWhiteSpace(version))
            {
                return;
            }

            var expectedTag = $"{packageId}/{version}";
            if (tags.Length == 0)
            {
                report.Add(AuditSeverity.Recommendation, "DASUPM018", $"Release tag verification is unavailable for expected tag '{expectedTag}'.", relativeManifestPath, "Run from a Git checkout or supply known tags to CI.");
            }
            else if (!tags.Contains(expectedTag, StringComparer.Ordinal))
            {
                report.Add(AuditSeverity.Warning, "DASUPM019", $"Expected package tag '{expectedTag}' was not found.", relativeManifestPath, "Create the package-specific tag after release validation succeeds.");
            }
        }

        private static string[] ReadLocalTags(string projectRoot)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"-C \"{projectRoot.Replace("\"", "\\\"")}\" tag --list",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return Array.Empty<string>();
                }

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return process.ExitCode == 0 ? output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).OrderBy(tag => tag, StringComparer.Ordinal).ToArray() : Array.Empty<string>();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        [Serializable]
        private sealed class ManifestDocument
        {
            public string name;
            public string version;
            public string displayName;
            public string description;
            public string unity;
            public string documentationUrl;
            public string changelogUrl;
            public string licensesUrl;
            public RepositoryDocument repository;
            public string[] testables;
        }

        [Serializable]
        private sealed class RepositoryDocument
        {
            public string url;
        }
    }
}
