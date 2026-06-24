using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Loqui.Editor
{
    public sealed class LocalizationScannerWindow : EditorWindow
    {
        private const string DefaultOutputFolder = "Assets/AIOutput/Localization/ScanOutput";

        private string _searchFolder = "Assets";
        private string _outputFolder = DefaultOutputFolder;
        private string _targetLanguage = LocalizationLanguageCodes.BrazilianPortuguese;
        private bool _includeScenes = true;
        private bool _includePrefabs = true;
        private bool _includeScripts;
        private Vector2 _scroll;
        private List<LocalizationScanItem> _results;

        [MenuItem("Tools/Loqui/Scan Texts")]
        private static void Open()
        {
            GetWindow<LocalizationScannerWindow>(true, "Localization Scanner", true);
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Review-only scan of TMP and legacy Text in scenes and prefabs. Code mutator hints are advisory " +
                "and make CodeApi win over component attach until reviewed. No assets are modified by scanning or exporting.",
                MessageType.Info);

            _searchFolder = EditorGUILayout.TextField("Search Folder", _searchFolder);
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
            _targetLanguage = EditorGUILayout.TextField("AI Target Language", _targetLanguage);
            _includeScenes = EditorGUILayout.Toggle("Include Scenes", _includeScenes);
            _includePrefabs = EditorGUILayout.Toggle("Include Prefabs", _includePrefabs);
            _includeScripts = EditorGUILayout.Toggle("Include Scripts (advisory)", _includeScripts);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan"))
                {
                    Scan();
                }

                using (new EditorGUI.DisabledScope(_results == null))
                {
                    if (GUILayout.Button("Export"))
                    {
                        Export();
                    }

                    if (GUILayout.Button("Attach ComponentAttach"))
                    {
                        AttachApproved();
                    }
                }
            }

            if (_results == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Results", _results.Count + " items");
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (var i = 0; i < _results.Count; i++)
            {
                var item = _results[i];
                var label = item.IsCandidate
                    ? item.RecommendedApproach + " " + item.ProposedKey
                    : "(" + item.RecommendedApproach + " " + item.ExclusionReason + ")";
                EditorGUILayout.LabelField(label, item.EnglishSource);
            }

            EditorGUILayout.EndScrollView();
        }

        private void Scan()
        {
            var options = new LocalizationScanOptions
            {
                SearchFolders = new[] { _searchFolder },
                IncludeScenes = _includeScenes,
                IncludePrefabs = _includePrefabs,
                IncludeScripts = _includeScripts
            };
            _results = LocalizationTextScanner.Scan(options);
        }

        private void Export()
        {
            var written = LocalizationScanExporter.ExportAll(_results, _outputFolder, _targetLanguage);
            Debug.Log("[Localization] Scan exported " + written.Count + " files to " + _outputFolder);
        }

        private void AttachApproved()
        {
            if (!EditorUtility.DisplayDialog(
                "Attach LocalizedText",
                "Attach LocalizedText only to scan results whose RecommendedApproach is ComponentAttach? " +
                "CodeApi and Conflict rows will be skipped and reported.",
                "Attach",
                "Cancel"))
            {
                return;
            }

            var report = LocalizationAttachMode.AttachApproved(_results);
            var attached = 0;
            for (var i = 0; i < report.Count; i++)
            {
                if (report[i].Action == LocalizationAttachMode.AttachedAction)
                {
                    attached++;
                }
            }

            Debug.Log("[Localization] Attach mode: " + attached + " attached, " + report.Count + " processed.");
        }
    }
}
