using DevAccelerationSystem.Core;
using UnityEditor;
using UnityEngine;

namespace DevAccelerationSystem.ProjectCompilationCheck
{
    
    internal sealed class CompilationOutputViewerEditor : EditorWindow
    {
        private CompilationOutput compilationOutput;
        private string locationOfCompilationOutput;
        private GUIStyle _styleLabel;
        private Vector2 scrollPosition1, scrollPosition2;
        private void OnEnable()
        {
           
            locationOfCompilationOutput = ProjectCompilationConfigSO.Find()?.DefaultCompilationOutputFileName;
            compilationOutput = FileUtility.LoadFromJson<CompilationOutput>(locationOfCompilationOutput);
        }

        public  void OnGUI()
        {
            EditorGUILayout.LabelField("Location of compilationOutput :"+locationOfCompilationOutput, EditorStyles.miniLabel);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            if (compilationOutput != null)
            {
                EditorGUILayout.LabelField("Total time compilation :"+compilationOutput.Stats.CompilationTotalMs+"ms", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                scrollPosition1 = EditorGUILayout.BeginScrollView(scrollPosition1);
                foreach (var result in compilationOutput.Results)
                {
                    if (_styleLabel == null)
                    {
                        _styleLabel = new GUIStyle(GUI.skin.label);
                        _styleLabel.richText = true;
                    }

                    EditorGUILayout.LabelField($"<b>{result.ProjectCompilationSettingName}:</b>", _styleLabel);
                    if (result.ErrorsCount > 0)
                    {
                        EditorGUILayout.HelpBox($"Compilation failed for {result.CompilationStats.CompilationTotalMs}ms with errors:\n"+result.ErrorsList
                                                , MessageType.Warning);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(
                            $"Compilation succeded for {result.CompilationStats.CompilationTotalMs}ms",  MessageType.Info);
                    }
                }
                EditorGUILayout.EndScrollView();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Logs:", EditorStyles.boldLabel);
                scrollPosition2 = EditorGUILayout.BeginScrollView(scrollPosition2);
                EditorGUILayout.HelpBox(compilationOutput.Logs,  MessageType.Info);
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("There are no compilation output at the moment. Please run compilation first.", MessageType.Warning);
            }
        }
    }
}