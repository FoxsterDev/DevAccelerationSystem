using System.Collections.Generic;
using System.IO;
using DevAccelerationSystem.Core;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;
using ILogger = DevAccelerationSystem.Core.ILogger;

namespace DevAccelerationSystem.ProjectCompilationCheck
{
    [CreateAssetMenuAttribute(fileName = "ProjectCompilationConfig", menuName = "Assets/DevAccelerationSystem/Create ProjectCompilationConfig", order = 100)]
    internal sealed class ProjectCompilationConfigSO :  ScriptableObject
    {
        public bool AutoOpenCompilationViewerWhenCompilationFinished = true;

        public List<CompilationConfig> CompilationConfigs = new List<CompilationConfig>()
        {
            new CompilationConfig
            {
                Name = BuildTarget.Android.ToString()+"Development",
                Option = ScriptCompilationOptions.DevelopmentBuild,
                Target = BuildTarget.Android,
                Enabled = true,
                ExtraScriptingDefines = new[]
                    {"JSONNET_XMLDISABLE"}
            },
            new CompilationConfig
            {
                Name = BuildTarget.Android.ToString()+"NotDevelopment",
                Option = ScriptCompilationOptions.None,
                Target = BuildTarget.Android,
                Enabled = true,
                ExtraScriptingDefines = new[]
                    {"JSONNET_XMLDISABLE"}
            },
            new CompilationConfig
            {
                Name = BuildTarget.iOS.ToString()+"Development",
                Option = ScriptCompilationOptions.DevelopmentBuild,
                Target = BuildTarget.iOS,
                Enabled = true,
                ExtraScriptingDefines = new[]
                    {"JSONNET_XMLDISABLE"}
            },
            new CompilationConfig
            {
                Name = BuildTarget.iOS.ToString()+"NotDevelopment",
                Option = ScriptCompilationOptions.None,
                Target = BuildTarget.iOS,
                Enabled = true,
                ExtraScriptingDefines = new[]
                    {"JSONNET_XMLDISABLE"}
            },
            new CompilationConfig
            {
                Name = BuildTarget.WebGL.ToString()+"Development",
                Option = ScriptCompilationOptions.DevelopmentBuild,
                Target = BuildTarget.WebGL,
                Enabled = true,
                ExtraScriptingDefines = new[]
                    {"JSONNET_XMLDISABLE"}
            },
            new CompilationConfig
            {
                Name = BuildTarget.WebGL.ToString()+"NotDevelopment",
                Option = ScriptCompilationOptions.None,
                Target = BuildTarget.WebGL,
                Enabled = true,
                ExtraScriptingDefines = new[]
                    {"JSONNET_XMLDISABLE"}
            },
            new CompilationConfig
            {
                Name = BuildTarget.StandaloneOSX.ToString()+"Development",
                Option = ScriptCompilationOptions.DevelopmentBuild,
                Target = BuildTarget.StandaloneOSX,
                Enabled = true,
                ExtraScriptingDefines = new[]
                    {"JSONNET_XMLDISABLE"}
            },
            new CompilationConfig
            {
                Name = BuildTarget.StandaloneOSX.ToString()+"NotDevelopment",
                Option = ScriptCompilationOptions.None,
                Target = BuildTarget.StandaloneOSX,
                Enabled = true,
                ExtraScriptingDefines = new[]
                    {"JSONNET_XMLDISABLE"}
            },
            new CompilationConfig
            {
                Name = BuildTarget.StandaloneWindows64.ToString()+"Development",
                Option = ScriptCompilationOptions.DevelopmentBuild,
                Target = BuildTarget.StandaloneWindows64,
                Enabled = true,
                ExtraScriptingDefines = new[]
                    {"JSONNET_XMLDISABLE"}
            },
            new CompilationConfig
            {
                Name = BuildTarget.StandaloneWindows64.ToString()+"NotDevelopment",
                Option = ScriptCompilationOptions.None,
                Target = BuildTarget.StandaloneWindows64,
                Enabled = true,
                ExtraScriptingDefines = new[]
                    {"JSONNET_XMLDISABLE"}
            }
        };
        
        public string DefaultCompilationOutputFileName = Path.Combine(Path.Combine("Library","ProjectCompilationCheckOutput"), "CompilationOutput.json");
        
        public static ProjectCompilationConfigSO Find(ILogger logger = null)
        {
            var so = ScriptableObjectExtension.LoadAllAssetsOfType<ProjectCompilationConfigSO>();
            switch (so.Count)
            {
                case 0:
                    logger?.Error("Could not find a project compilation config so!");
                    return null;
                case 1:
                    return so[0];
                default:
                    logger?.Warning($"For editor usage you can specify few configs, but in batch mode only one is allowed. " +
                                    $"Using the first one with name {so[0].name}.");
                    return so[0];
            }
        }
    }
}