using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DevAccelerationSystem.ProjectAuditing
{
    public enum AuditSeverity
    {
        Error,
        Warning,
        Recommendation
    }

    [Serializable]
    public sealed class AuditFinding
    {
        public string Code;
        public AuditSeverity Severity;
        public string Message;
        public string Path;
        public string Remediation;
    }

    [Serializable]
    public sealed class AuditMetric
    {
        public string Name;
        public string Value;
    }

    [Serializable]
    public sealed class AuditReport
    {
        public const string CurrentSchemaVersion = "1";

        public string SchemaVersion = CurrentSchemaVersion;
        public string AuditName;
        public string ProjectRoot;
        public List<AuditFinding> Findings = new List<AuditFinding>();
        public List<AuditMetric> Metrics = new List<AuditMetric>();

        public bool HasErrors => Findings.Any(finding => finding.Severity == AuditSeverity.Error);

        public void Add(AuditSeverity severity, string code, string message, string path = "", string remediation = "")
        {
            Findings.Add(new AuditFinding
            {
                Code = code,
                Severity = severity,
                Message = message,
                Path = path ?? string.Empty,
                Remediation = remediation ?? string.Empty
            });
        }

        public void AddMetric(string name, object value)
        {
            Metrics.Add(new AuditMetric
            {
                Name = name,
                Value = value?.ToString() ?? string.Empty
            });
        }

        public string ToJson()
        {
            Findings = Findings.OrderBy(finding => finding.Severity).ThenBy(finding => finding.Code, StringComparer.Ordinal).ThenBy(finding => finding.Path, StringComparer.Ordinal).ThenBy(finding => finding.Message, StringComparer.Ordinal).ToList();
            Metrics = Metrics.OrderBy(metric => metric.Name, StringComparer.Ordinal).ThenBy(metric => metric.Value, StringComparer.Ordinal).ToList();
            return JsonUtility.ToJson(this, true);
        }

        public void Save(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("An output path is required.", nameof(path));
            }

            var directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, ToJson());
        }
    }

    public static class ProjectAuditPaths
    {
        public static string GetProjectRoot()
        {
            return Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
        }

        public static string ToProjectRelativePath(string projectRoot, string fullPath)
        {
            if (string.IsNullOrEmpty(projectRoot) || string.IsNullOrEmpty(fullPath))
            {
                return string.Empty;
            }

            var normalizedRoot = System.IO.Path.GetFullPath(projectRoot).TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
            var normalizedPath = System.IO.Path.GetFullPath(fullPath);
            if (!normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedPath.Replace(System.IO.Path.DirectorySeparatorChar, '/');
            }

            return normalizedPath.Substring(normalizedRoot.Length).Replace(System.IO.Path.DirectorySeparatorChar, '/');
        }

        public static bool IsInside(string root, string path)
        {
            var normalizedRoot = System.IO.Path.GetFullPath(root).TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
            return System.IO.Path.GetFullPath(path).StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }
    }
}
