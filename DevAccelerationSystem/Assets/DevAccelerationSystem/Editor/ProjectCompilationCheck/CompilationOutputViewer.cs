using System;
using DevAccelerationSystem.Core;
using UnityEditor;
using UnityEngine;

namespace DevAccelerationSystem.ProjectCompilationCheck
{
    
    internal sealed class CompilationOutputViewerEditor : EditorWindow
    {
        private CompilationOutput _compilationOutput;
        private string _locationOfCompilationOutput;
        private GUIStyle _styleLabel;
        private Vector2 _scrollPosition1, _scrollPosition2;

        private void OnFocus()
        {
            _compilationOutput = FileUtility.LoadFromJson<CompilationOutput>(_locationOfCompilationOutput);
        }

        private void OnEnable()
        {
           
            _locationOfCompilationOutput = ProjectCompilationConfigSO.Find()?.DefaultCompilationOutputFileName;
            _compilationOutput = FileUtility.LoadFromJson<CompilationOutput>(_locationOfCompilationOutput);
        }

        public  void OnGUI()
        {
            EditorGUILayout.LabelField($"Location of compilationOutput :{_locationOfCompilationOutput}", EditorStyles.miniLabel);
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (_compilationOutput != null)
            {
                EditorGUILayout.LabelField($"Total time compilation :{_compilationOutput.Stats.CompilationTotalMs}ms", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                _scrollPosition1 = EditorGUILayout.BeginScrollView(_scrollPosition1);
                foreach (var result in _compilationOutput.Results)
                {
                    if (_styleLabel == null)
                    {
                        _styleLabel = new GUIStyle(GUI.skin.label)
                        {
                            richText = true
                        };
                    }

                    EditorGUILayout.LabelField($"<b>{result.ProjectCompilationSettingName}:</b>", _styleLabel);
                    if (result.ErrorsCount > 0)
                    {
                        EditorGUILayout.HelpBox($"Compilation failed for {result.CompilationStats.CompilationTotalMs}ms with {result.ErrorsCount} errors:\n"+result.ErrorsList
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
                _scrollPosition2 = EditorGUILayout.BeginScrollView(_scrollPosition2);
                EditorGUILayout.HelpBox(_compilationOutput.Logs,  MessageType.Info);
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("There are no compilation output at the moment. Please run compilation first.", MessageType.Warning);
            }
        }
    }
}