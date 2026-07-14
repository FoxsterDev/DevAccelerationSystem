using System;
using System.IO;
using System.Linq;
using DevAccelerationSystem.ProjectAuditing;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DevAccelerationSystem.Tests.Editor
{
    [Category("DevAccelerationSystem")]
    public sealed class ProjectAuditingTests
    {
        private string _projectRoot;

        [SetUp]
        public void SetUp()
        {
            _projectRoot = Path.Combine(Path.GetTempPath(), "DevAccelerationSystemProjectAuditing", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_projectRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_projectRoot))
            {
                Directory.Delete(_projectRoot, true);
            }
        }

        [Test]
        public void UpmPackageDoctor_ValidPackage_HasNoErrors()
        {
            var packageRoot = CreatePackage("com.example.sample", "1.2.3", true);
            var report = UpmPackageDoctor.AnalyzeProject(_projectRoot, new PackageDoctorOptions
            {
                KnownTags = new[] { "com.example.sample/1.2.3" }
            });

            Assert.That(packageRoot, Is.Not.Empty);
            Assert.That(report.HasErrors, Is.False);
            Assert.That(report.Findings.Any(finding => finding.Code == "DASUPM020"), Is.False);
        }

        [Test]
        public void UpmPackageDoctor_InvalidPackage_ReportsManifestAndMetadataErrors()
        {
            var packageRoot = Path.Combine(_projectRoot, "Assets", "BrokenPackage");
            Directory.CreateDirectory(packageRoot);
            File.WriteAllText(Path.Combine(packageRoot, "package.json"), "{\"name\":\"invalid\",\"version\":\"not-semver\",\"dependencies\":[]}");

            var report = UpmPackageDoctor.AnalyzeProject(_projectRoot, new PackageDoctorOptions
            {
                KnownTags = new[] { "invalid/1.0.0" }
            });

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Findings.Select(finding => finding.Code), Does.Contain("DASUPM002"));
            Assert.That(report.Findings.Select(finding => finding.Code), Does.Contain("DASUPM003"));
            Assert.That(report.Findings.Select(finding => finding.Code), Does.Contain("DASUPM005"));
            Assert.That(report.Findings.Select(finding => finding.Code), Does.Contain("DASUPM020"));
        }

        [Test]
        public void UpmPackageDoctor_CanonicalPackages_HaveNoErrorsForProposedReleaseTags()
        {
            var sourceProjectRoot = Directory.GetParent(Application.dataPath).FullName;
            var report = UpmPackageDoctor.AnalyzeProject(sourceProjectRoot, new PackageDoctorOptions
            {
                KnownTags = new[]
                {
                    "com.foxsterdev.devaccelerationsystem/1.1.0",
                    "com.foxsterdev.loqui/0.3.2",
                    "com.foxsterdev.thebestlogger/4.4.2"
                }
            });

            Assert.That(report.HasErrors, Is.False, report.ToJson());
            Assert.That(report.Metrics.Single(metric => metric.Name == "packagesDiscovered").Value, Is.EqualTo("3"));
        }

        [Test]
        public void UpmPackageDoctor_AbsolutePathInPackageSource_IsReported()
        {
            var packageRoot = CreatePackage("com.example.sample", "1.2.3", true);
            File.WriteAllText(Path.Combine(packageRoot, "Runtime.cs"), "public sealed class Sample { private const string Path = \"/Users/example/secret\"; }");

            var report = UpmPackageDoctor.AnalyzeProject(_projectRoot, new PackageDoctorOptions
            {
                KnownTags = new[] { "com.example.sample/1.2.3" }
            });

            Assert.That(report.Findings.Select(finding => finding.Code), Does.Contain("DASUPM017"));
        }

        [Test]
        public void DefineDoctor_ReportsDriftRequiredAndForbiddenSymbols()
        {
            var asmdefDirectory = Path.Combine(_projectRoot, "Assets", "Assembly");
            Directory.CreateDirectory(asmdefDirectory);
            File.WriteAllText(Path.Combine(asmdefDirectory, "Example.asmdef"), "{\"name\":\"Example\",\"defineConstraints\":[\"UNDECLARED\"],\"versionDefines\":[{\"name\":\"\",\"expression\":\"\",\"define\":\"\"}]}");
            var options = new DefineDoctorOptions
            {
                RequiredSymbols = new[] { "REQUIRED" },
                ForbiddenSymbols = new[] { "FORBIDDEN" },
                RequiredBuildProfileNames = new[] { "MissingProfile" }
            };
            var report = DefineBuildProfileDoctor.AnalyzeSnapshots(_projectRoot, options, new[]
            {
                new DefineSnapshot { TargetGroup = BuildTargetGroup.Android, Symbols = new[] { "COMMON" } },
                new DefineSnapshot { TargetGroup = BuildTargetGroup.Standalone, Symbols = new[] { "COMMON", "FORBIDDEN" } }
            });

            Assert.That(report.Findings.Select(finding => finding.Code), Does.Contain("DASDEFINE001"));
            Assert.That(report.Findings.Select(finding => finding.Code), Does.Contain("DASDEFINE002"));
            Assert.That(report.Findings.Select(finding => finding.Code), Does.Contain("DASDEFINE003"));
            Assert.That(report.Findings.Select(finding => finding.Code), Does.Contain("DASDEFINE004"));
            Assert.That(report.Findings.Select(finding => finding.Code), Does.Contain("DASDEFINE005"));
            Assert.That(report.Findings.Select(finding => finding.Code), Does.Contain("DASDEFINE006"));
        }

        [Test]
        public void DefineDoctor_Preview_OnlyProposesRequiredAndForbiddenChanges()
        {
            var preview = DefineBuildProfileDoctor.CreatePreview(new DefineDoctorOptions
            {
                RequiredSymbols = new[] { "REQUIRED" },
                ForbiddenSymbols = new[] { "FORBIDDEN" }
            }, new[]
            {
                new DefineSnapshot { TargetGroup = BuildTargetGroup.Android, Symbols = new[] { "FORBIDDEN", "EXISTING" } }
            });

            Assert.That(preview.Changes, Has.Count.EqualTo(1));
            Assert.That(preview.Changes[0].PreviousSymbols, Is.EqualTo(new[] { "EXISTING", "FORBIDDEN" }));
            Assert.That(preview.Changes[0].ProposedSymbols, Is.EqualTo(new[] { "EXISTING", "REQUIRED" }));
        }

        [Test]
        public void ProjectBaselineAudit_PolicyViolations_AreReportedDeterministically()
        {
            var policy = new BaselinePolicy
            {
                RequiredColorSpace = "Linear",
                RequiredPackages = new[] { new BaselinePackage { Name = "com.example.required", Version = "1.0.0" } },
                ForbiddenPackageIds = new[] { "com.example.forbidden" },
                RequiredProjectFiles = new[] { "Assets/Required.asset" },
                Targets = new[]
                {
                    new BaselineTargetPolicy
                    {
                        TargetGroup = nameof(BuildTargetGroup.Android),
                        ScriptingBackend = "IL2CPP",
                        RequiredSymbols = new[] { "REQUIRED" }
                    }
                }
            };
            var context = new BaselineContext
            {
                ProjectRoot = _projectRoot,
                ColorSpace = "Gamma",
                Packages = new[] { new BaselinePackage { Name = "com.example.forbidden", Version = "1.0.0" } },
                ExistingProjectFiles = Array.Empty<string>(),
                Targets = new[]
                {
                    new BaselineTargetSnapshot
                    {
                        TargetGroup = BuildTargetGroup.Android,
                        ScriptingBackend = "Mono2x",
                        Symbols = Array.Empty<string>()
                    }
                }
            };

            var report = ProjectBaselineAudit.Evaluate(policy, context);

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Findings.Select(finding => finding.Code), Does.Contain("DASBASE005"));
            Assert.That(report.Findings.Select(finding => finding.Code), Does.Contain("DASBASE007"));
            Assert.That(report.Findings.Select(finding => finding.Code), Does.Contain("DASBASE010"));
            Assert.That(report.ToJson(), Is.EqualTo(report.ToJson()));
        }

        [Test]
        public void ProjectBaselineAudit_ReadsLowerCamelCasePolicySchema()
        {
            var requiredFile = Path.Combine(_projectRoot, "Assets", "Required.asset");
            Directory.CreateDirectory(Path.GetDirectoryName(requiredFile));
            File.WriteAllText(requiredFile, "required");
            var baselinePath = Path.Combine(_projectRoot, "ProjectBaseline.json");
            File.WriteAllText(baselinePath, "{\"schemaVersion\":\"1\",\"requiredProjectFiles\":[\"Assets/Required.asset\"]}");

            var report = ProjectBaselineAudit.AnalyzeProject(baselinePath, _projectRoot);

            Assert.That(report.HasErrors, Is.False, report.ToJson());
        }

        [Test]
        public void ProjectBaselineAudit_InvalidPolicy_ReturnsAnAuditError()
        {
            var baselinePath = Path.Combine(_projectRoot, "ProjectBaseline.json");
            File.WriteAllText(baselinePath, "{");

            var report = ProjectBaselineAudit.AnalyzeProject(baselinePath, _projectRoot);

            Assert.That(report.Findings.Select(finding => finding.Code), Does.Contain("DASBASE002"));
        }

        [Test]
        public void ProjectBaselineAudit_InvalidColorSpace_IsReportedBeforeRemediation()
        {
            var report = ProjectBaselineAudit.Evaluate(new BaselinePolicy
            {
                RequiredColorSpace = "InvalidColorSpace"
            }, new BaselineContext
            {
                ProjectRoot = _projectRoot,
                ColorSpace = "Gamma"
            });

            Assert.That(report.Findings.Select(finding => finding.Code), Does.Contain("DASBASE014"));
        }

        [Test]
        public void ProjectDoctorRunner_UnsupportedCommand_ReturnsActionableError()
        {
            var report = ProjectDoctorRunner.Execute(new[] { "-dasDoctor", "unsupported" });

            Assert.That(report.Findings.Select(finding => finding.Code), Does.Contain("DASRUN001"));
        }

        [Test]
        public void ProjectDoctorRunner_UpmCommand_RunsTheReadOnlyAudit()
        {
            var report = ProjectDoctorRunner.Execute(new[] { "-dasDoctor", "upm" });

            Assert.That(report.AuditName, Is.EqualTo("UPM Package Doctor"));
            Assert.That(report.HasErrors, Is.False, report.ToJson());
        }

        [Test]
        public void AuditReport_ToJson_SortsFindingsDeterministically()
        {
            var report = new AuditReport();
            report.Add(AuditSeverity.Warning, "B", "second", "B");
            report.Add(AuditSeverity.Error, "A", "first", "A");

            var json = report.ToJson();

            Assert.That(json.IndexOf("\"Code\": \"A\"", StringComparison.Ordinal), Is.LessThan(json.IndexOf("\"Code\": \"B\"", StringComparison.Ordinal)));
        }

        private string CreatePackage(string packageId, string version, bool includeMetadata)
        {
            var packageRoot = Path.Combine(_projectRoot, "Assets", "SamplePackage");
            Directory.CreateDirectory(Path.Combine(packageRoot, "Tests", "Editor"));
            File.WriteAllText(Path.Combine(packageRoot, "package.json"), $"{{\"name\":\"{packageId}\",\"version\":\"{version}\",\"displayName\":\"Sample\",\"description\":\"Sample package\",\"unity\":\"2022.3\",\"documentationUrl\":\"https://example.com/docs\",\"changelogUrl\":\"https://example.com/changelog\",\"licensesUrl\":\"https://example.com/license\",\"repository\":{{\"url\":\"https://example.com/repository.git\"}},\"dependencies\":{{\"com.unity.textmeshpro\":\"3.0.7\"}},\"testables\":[\"Sample.Tests\"]}}");
            File.WriteAllText(Path.Combine(packageRoot, "Tests", "Editor", "Sample.Tests.asmdef"), "{\"name\":\"Sample.Tests\"}");
            if (includeMetadata)
            {
                File.WriteAllText(Path.Combine(packageRoot, "README.md"), "readme");
                File.WriteAllText(Path.Combine(packageRoot, "CHANGELOG.md"), "changelog");
                File.WriteAllText(Path.Combine(packageRoot, "LICENSE.md"), "license");
            }

            return packageRoot;
        }
    }
}
