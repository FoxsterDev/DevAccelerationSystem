using DevAccelerationSystem.Core;
using UnityEditor;

namespace DevAccelerationSystem.ProjectCompilationCheck
{
    [CustomEditor(typeof(CompilationOutputViewerSO))]
    internal sealed class CompilationOutputViewerSOEditor : UnityEditor.Editor
    {
        private CompilationOutput compilationOutput;
        private string locationOfCompilationOutput;
        
        private void OnEnable()
        {
            locationOfCompilationOutput = ProjectCompilationConfigSO.Instance.DefaultCompilationOutputFileName;
            compilationOutput = FileUtility.LoadFromJson<CompilationOutput>(locationOfCompilationOutput);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Location of compilationOutput :"+locationOfCompilationOutput, EditorStyles.miniLabel);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            if (compilationOutput != null)
            {
                EditorGUILayout.LabelField("Total time compilation :"+compilationOutput.Stats.CompilationTotalMs+"ms", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                foreach (var result in compilationOutput.Results)
                {
                    if (result.ErrorsCount > 0)
                    {
                        EditorGUILayout.HelpBox($"{result.ProjectCompilationSettingName}: Compilation failed for {result.CompilationStats.CompilationTotalMs}ms with errors:\n"+result.ErrorsList
                                                , MessageType.Warning);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(
                            $"{result.ProjectCompilationSettingName}: Compilation succeded for {result.CompilationStats.CompilationTotalMs}ms",  MessageType.Info);
                    }
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Logs:", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(compilationOutput.Logs,  MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("There are no compilation output at the moment. Please run compilation first.", MessageType.Warning);
            }
        }
    }
}