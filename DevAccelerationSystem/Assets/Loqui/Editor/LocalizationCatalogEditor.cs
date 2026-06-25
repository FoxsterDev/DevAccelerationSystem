using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Loqui.Editor
{
    [CustomEditor(typeof(LocalizationCatalog))]
    public sealed class LocalizationCatalogEditor : UnityEditor.Editor
    {
        enum UsageFilter
        {
            All,
            UsedOnly,
            UnusedOnly,
            OverridesOnly
        }

        static readonly string[] ViewLabels = { "By Key", "By Module", "By Group", "Generic API" };
        static readonly string[] FilterLabels = { "All", "Used only", "Unused only", "Overrides only" };

        readonly LocalizationUsageScanner _scanner = new();
        bool _usageFoldout;
        LocalizationUsageView _view = LocalizationUsageView.ByKey;
        UsageFilter _filter = UsageFilter.All;
        Vector2 _scroll;
        bool _ticking;

        void OnDisable()
        {
            StopTicking();
            _scanner.Cancel();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var catalog = (LocalizationCatalog)target;
            EditorGUILayout.Space();

            if (catalog.IsValid(out var error))
            {
                EditorGUILayout.HelpBox(
                    $"Valid · {Count(catalog.Languages)} language(s) · {Count(catalog.Texts)} text key(s) · {Count(catalog.Bools)} bool flag(s)",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(error, MessageType.Warning);
            }

            EditorGUILayout.Space();
            DrawAdvancedUsage(catalog);
        }

        void DrawAdvancedUsage(LocalizationCatalog catalog)
        {
            _usageFoldout = EditorGUILayout.Foldout(_usageFoldout, "Advanced Usage", true, EditorStyles.foldoutHeader);
            if (!_usageFoldout)
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_scanner.IsRunning))
                {
                    if (GUILayout.Button("Scan Usages", GUILayout.Width(110f)))
                    {
                        StartScan(catalog);
                    }
                }

                using (new EditorGUI.DisabledScope(!_scanner.IsRunning))
                {
                    if (GUILayout.Button("Stop", GUILayout.Width(70f)))
                    {
                        _scanner.Cancel();
                        StopTicking();
                    }
                }

                GUILayout.Label(_scanner.Report != null ? _scanner.Report.Status : "Not scanned yet.", EditorStyles.miniLabel);
            }

            if (_scanner.IsRunning)
            {
                var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                EditorGUI.ProgressBar(rect, _scanner.Progress, $"Scanning… {_scanner.Progress * 100f:0}%");
                return;
            }

            var report = _scanner.Report;
            if (report == null || !report.Completed)
            {
                EditorGUILayout.HelpBox(
                    "Scans the project for Loc.Get / GetBool calls (literal and const-key) and LocalizedText bindings, then reports per-key usage so you can spot unused keys. Runs on demand.",
                    MessageType.None);
                return;
            }

            DrawStats(report);
            _view = (LocalizationUsageView)GUILayout.Toolbar((int)_view, ViewLabels);
            if (_view != LocalizationUsageView.GenericApi)
            {
                _filter = (UsageFilter)GUILayout.Toolbar((int)_filter, FilterLabels);
            }

            EditorGUILayout.Space(2f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(420f));
            switch (_view)
            {
                case LocalizationUsageView.GenericApi:
                    DrawGeneric(report);
                    break;
                case LocalizationUsageView.ByModule:
                    DrawGrouped(report, byModule: true);
                    break;
                case LocalizationUsageView.ByGroup:
                    DrawGrouped(report, byModule: false);
                    break;
                default:
                    EditorGUILayout.LabelField("Config Entry Usages", EditorStyles.boldLabel);
                    foreach (var usage in report.Keys)
                    {
                        if (PassesFilter(usage))
                        {
                            DrawKeyRow(usage);
                        }
                    }

                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawStats(LocalizationUsageReport report)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                Chip($"Calls {report.TotalCalls}");
                Chip($"Used {report.UsedCount}/{report.CatalogKeyCount}");
                Chip($"Unused {report.UnusedCount}");
                Chip($"Generic {report.Generic.Count}");
                Chip($"Modules {report.Modules.Count}");
                Chip($"Overrides {report.OverrideCount}");
            }
        }

        static void Chip(string text)
        {
            GUILayout.Label(text, EditorStyles.miniButton, GUILayout.Height(20f));
        }

        bool PassesFilter(LocalizationKeyUsage usage)
        {
            switch (_filter)
            {
                case UsageFilter.UsedOnly:
                    return usage.Used;
                case UsageFilter.UnusedOnly:
                    return !usage.Used;
                case UsageFilter.OverridesOnly:
                    return usage.HasOverride;
                default:
                    return true;
            }
        }

        void DrawKeyRow(LocalizationKeyUsage usage)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var title = usage.IsBool ? $"{usage.Key}  (bool)" : usage.Key;
                    if (!string.IsNullOrEmpty(usage.English))
                    {
                        title += "  —  " + Ellipsis(usage.English, 48);
                    }

                    EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    var badge = usage.Used ? usage.CallCount.ToString() : "unused";
                    GUILayout.Label(badge, EditorStyles.miniButton, GUILayout.MinWidth(48f));
                }

                var modules = usage.Modules.Count == 0 ? "—" : string.Join(", ", usage.Modules);
                var files = usage.Files.Count == 0 ? "—" : string.Join(", ", usage.Files);
                var flags = usage.HasOverride ? "  ·  has overrides" : string.Empty;
                EditorGUILayout.LabelField($"{usage.CallCount} calls{flags}  |  Modules: {modules}  |  Files: {files}", EditorStyles.miniLabel);
            }
        }

        void DrawGrouped(LocalizationUsageReport report, bool byModule)
        {
            var groups = new SortedDictionary<string, List<LocalizationKeyUsage>>(System.StringComparer.Ordinal);
            foreach (var usage in report.Keys)
            {
                if (!PassesFilter(usage))
                {
                    continue;
                }

                if (byModule)
                {
                    if (usage.Modules.Count == 0)
                    {
                        Bucket(groups, "(unused)").Add(usage);
                    }
                    else
                    {
                        foreach (var module in usage.Modules)
                        {
                            Bucket(groups, module).Add(usage);
                        }
                    }
                }
                else
                {
                    Bucket(groups, string.IsNullOrEmpty(usage.Group) ? "(ungrouped)" : usage.Group).Add(usage);
                }
            }

            foreach (var pair in groups)
            {
                EditorGUILayout.LabelField($"{pair.Key}  ({pair.Value.Count})", EditorStyles.boldLabel);
                foreach (var usage in pair.Value)
                {
                    DrawKeyRow(usage);
                }

                EditorGUILayout.Space(2f);
            }
        }

        void DrawGeneric(LocalizationUsageReport report)
        {
            EditorGUILayout.LabelField("Generic API calls (dynamic key — not attributable)", EditorStyles.boldLabel);
            if (report.Generic.Count == 0)
            {
                EditorGUILayout.HelpBox("No dynamic-key Loc calls found. Every call site resolves to a literal or const key.", MessageType.None);
                return;
            }

            foreach (var site in report.Generic)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    var arg = string.IsNullOrEmpty(site.Snippet) ? "<expression>" : site.Snippet;
                    EditorGUILayout.LabelField($"{site.FileName}:{site.Line}  ·  {site.Module}  ·  arg: {arg}", EditorStyles.miniLabel);
                    if (GUILayout.Button("→", GUILayout.Width(24f)))
                    {
                        Ping(site.AssetPath);
                    }
                }
            }
        }

        static List<LocalizationKeyUsage> Bucket(SortedDictionary<string, List<LocalizationKeyUsage>> groups, string key)
        {
            if (!groups.TryGetValue(key, out var list))
            {
                list = new List<LocalizationKeyUsage>();
                groups[key] = list;
            }

            return list;
        }

        static void Ping(string assetPath)
        {
            var obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (obj != null)
            {
                EditorGUIUtility.PingObject(obj);
            }
        }

        static string Ellipsis(string value, int max)
        {
            value = value.Replace("\n", " ").Replace("\u2028", " ").Replace("\u2029", " ");
            return value.Length <= max ? value : value.Substring(0, max - 1) + "…";
        }

        void StartScan(LocalizationCatalog catalog)
        {
            _scanner.Begin(catalog);
            StartTicking();
        }

        void StartTicking()
        {
            if (_ticking)
            {
                return;
            }

            _ticking = true;
            EditorApplication.update += OnEditorUpdate;
        }

        void StopTicking()
        {
            if (!_ticking)
            {
                return;
            }

            _ticking = false;
            EditorApplication.update -= OnEditorUpdate;
        }

        void OnEditorUpdate()
        {
            if (!_scanner.IsRunning)
            {
                StopTicking();
                Repaint();
                return;
            }

            _scanner.Step();
            Repaint();
        }

        static int Count<T>(List<T> list)
        {
            return list?.Count ?? 0;
        }
    }
}
