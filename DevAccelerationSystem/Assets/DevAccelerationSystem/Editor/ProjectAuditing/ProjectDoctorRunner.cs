using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DevAccelerationSystem.ProjectAuditing
{
    public static class ProjectDoctorRunner
    {
        public static void Run()
        {
            var report = Execute(Environment.GetCommandLineArgs());
            var outputPath = GetArgument("-dasDoctorOutput") ?? Path.Combine(ProjectAuditPaths.GetProjectRoot(), "Library", "DevAccelerationSystem", "Reports", $"{ToFileName(report.AuditName)}.json");
            report.Save(outputPath);
            Debug.Log($"{report.AuditName} report written to {outputPath}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(report.HasErrors ? 1 : 0);
            }
        }

        public static AuditReport Execute(string[] commandLineArguments)
        {
            var projectRoot = ProjectAuditPaths.GetProjectRoot();
            var command = GetArgument("-dasDoctor", commandLineArguments);
            switch (command)
            {
                case "upm":
                    return UpmPackageDoctor.AnalyzeProject(projectRoot);
                case "defines":
                    return DefineBuildProfileDoctor.AnalyzeProject(projectRoot, new DefineDoctorOptions
                    {
                        RequiredSymbols = SplitArgument("-dasRequiredSymbols", commandLineArguments),
                        ForbiddenSymbols = SplitArgument("-dasForbiddenSymbols", commandLineArguments),
                        RequiredBuildProfileNames = SplitArgument("-dasRequiredBuildProfiles", commandLineArguments)
                    });
                case "baseline":
                    var baselinePath = GetArgument("-dasBaseline", commandLineArguments);
                    if (!string.IsNullOrWhiteSpace(baselinePath) && !Path.IsPathRooted(baselinePath))
                    {
                        baselinePath = Path.Combine(projectRoot, baselinePath);
                    }

                    return ProjectBaselineAudit.AnalyzeProject(baselinePath, projectRoot);
                default:
                    var report = new AuditReport
                    {
                        AuditName = "Project Doctor Runner",
                        ProjectRoot = projectRoot
                    };
                    report.Add(AuditSeverity.Error, "DASRUN001", "Use -dasDoctor upm, defines, or baseline.", string.Empty, "Provide a supported Doctor command.");
                    return report;
            }
        }

        private static string GetArgument(string name, string[] arguments = null)
        {
            var values = arguments ?? Environment.GetCommandLineArgs();
            for (var index = 0; index < values.Length - 1; index++)
            {
                if (string.Equals(values[index], name, StringComparison.Ordinal))
                {
                    return values[index + 1];
                }
            }

            return null;
        }

        private static string[] SplitArgument(string name, string[] arguments)
        {
            var value = GetArgument(name, arguments);
            return string.IsNullOrWhiteSpace(value)
                ? Array.Empty<string>()
                : value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(symbol => symbol.Trim()).Where(symbol => !string.IsNullOrEmpty(symbol)).ToArray();
        }

        private static string ToFileName(string value)
        {
            return string.Join("_", (value ?? "report").Split(Path.GetInvalidFileNameChars())).Replace(' ', '_').ToLowerInvariant();
        }
    }

    internal sealed class ProjectDoctorWindow : EditorWindow
    {
        private string _baselinePath = "Assets/ProjectBaseline.json";
        private Vector2 _scrollPosition;
        private string _lastReport;

        [MenuItem("Window/DevAccelerationSystem/Project Doctors", false, 20)]
        private static void ShowWindow()
        {
            GetWindow<ProjectDoctorWindow>("Project Doctors");
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("All Doctor scans are read-only. Preview and apply changes through the public APIs only after reviewing the generated report.", MessageType.Info);
            if (GUILayout.Button("Run UPM Package Doctor"))
            {
                ShowReport(UpmPackageDoctor.AnalyzeProject());
            }

            if (GUILayout.Button("Run Define & Build Profile Doctor"))
            {
                ShowReport(DefineBuildProfileDoctor.AnalyzeProject());
            }

            _baselinePath = EditorGUILayout.TextField("Baseline Path", _baselinePath);
            if (GUILayout.Button("Run Project Baseline Audit"))
            {
                var projectRoot = ProjectAuditPaths.GetProjectRoot();
                var path = Path.IsPathRooted(_baselinePath) ? _baselinePath : Path.Combine(projectRoot, _baselinePath);
                ShowReport(ProjectBaselineAudit.AnalyzeProject(path, projectRoot));
            }

            if (!string.IsNullOrEmpty(_lastReport))
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                EditorGUILayout.TextArea(_lastReport, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }

        private void ShowReport(AuditReport report)
        {
            var outputPath = Path.Combine(ProjectAuditPaths.GetProjectRoot(), "Library", "DevAccelerationSystem", "Reports", $"{ToFileName(report.AuditName)}.json");
            report.Save(outputPath);
            _lastReport = report.ToJson();
            Debug.Log($"{report.AuditName} report written to {outputPath}");
        }

        private static string ToFileName(string value)
        {
            return string.Join("_", (value ?? "report").Split(Path.GetInvalidFileNameChars())).Replace(' ', '_').ToLowerInvariant();
        }
    }
}
