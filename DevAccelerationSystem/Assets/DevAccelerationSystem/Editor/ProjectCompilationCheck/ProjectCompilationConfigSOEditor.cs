using DevAccelerationSystem.Core;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace DevAccelerationSystem.ProjectCompilationCheck
{
    [CustomEditor(typeof(ProjectCompilationConfigSO))]
    internal sealed class ProjectCompilationConfigSOEditor : UnityEditor.Editor
    {
        private ProjectCompilationConfigSO ConfigSO => (ProjectCompilationConfigSO) target;
        private string _labelStats = "";
        
        private void OnEnable()
        {
            _labelStats = $"CodeOptimization: {CompilationPipeline.codeOptimization}, " +
                     $"EditorAssemblies: {CompilationPipeline.GetAssemblies(AssembliesType.Editor).Length}, " +
                     $"PlayerAssemblies: {CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies).Length}";
            _labelStats += "\n\nSetting scripting define symbols in batch mode will not work immediately\nbecause the asynchronous nature of Unity’s compilation process ";
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(_labelStats, MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Custom scripting symbols.", EditorStyles.linkLabel))
            {
               Application.OpenURL("https://docs.unity3d.com/Manual/CustomScriptingSymbols.html");
            }
            if(GUILayout.Button("List of Unity available scripting symbols.", EditorStyles.linkLabel))
            {
                Application.OpenURL("https://docs.unity3d.com/Manual/PlatformDependentCompilation.html");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            
            var enabled = !(EditorApplication.isCompiling ||
                             EditorApplication.isPlayingOrWillChangePlaymode ||
                             EditorApplication.isUpdating);

            GUI.enabled = enabled;
            EditorGUILayout.LabelField("Available compilation options:", EditorStyles.boldLabel);
            if (GUILayout.Button("Run All"))
            {
                var logger = new DefaultUnityLogger(nameof(ProjectCompilationCheck), 40000);
                var compilationOutput = EditorModeRunner.RunAll(ConfigSO.CompilationConfigs, logger);
                FileUtility.SaveAsJson(compilationOutput, ConfigSO.DefaultCompilationOutputFileName);
                return;
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            foreach (var config in ConfigSO.CompilationConfigs)
            {
                enabled = config.Enabled;
                var buildTargetSupported = config.Target.IsBuildTargetSupported();
                if (enabled && buildTargetSupported)
                {
                    if (GUILayout.Button("Run " + config.Name))
                    {
                        var logger = new DefaultUnityLogger(nameof(ProjectCompilationCheck), 40000);
                        var compilationOutput = EditorModeRunner.Run(config, logger);
                        FileUtility.SaveAsJson(compilationOutput, ConfigSO.DefaultCompilationOutputFileName);
                        return;
                    }
                }
                else if (!enabled && !buildTargetSupported)
                {
                    EditorGUILayout.LabelField(
                        $"Config {config.Name} is disabled and build target {config.Target} is not installed!");
                }
                else if (enabled)
                {
                    EditorGUILayout.LabelField(
                        $"Config {config.Name} for build target {config.Target} is not supported. Try install it!");
                }
                else
                {
                    EditorGUILayout.LabelField(
                        $"Config {config.Name} is disabled!");
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Other functions:", EditorStyles.boldLabel);
            if (GUILayout.Button("Full rebuild of all scripts"))
            {
                #if UNITY_2022_3_OR_NEWER
                CompilationPipeline.RequestScriptCompilation((RequestScriptCompilationOptions.CleanBuildCache));
                #else
                CompilationPipeline.RequestScriptCompilation();
                #endif
                return;
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("List of current configurations:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Do not forget to save the changes when modify the config! Like Cmd/Ctrl+S", MessageType.Info);
            
            serializedObject.Update();
            
            base.OnInspectorGUI();

            serializedObject.ApplyModifiedProperties();
            //ProjectCompilationConfigSO.ResetInstance();
            /*if (serializedObject.targetObject .hasModifiedProperties)// EditorUtility.IsDirty(target))
            {
                Debug.Log("!!!!");
                //ProjectCompilationConfigSO.SaveChangesInUnityEditor(false);
            }*/
        }
    }
}