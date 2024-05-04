using System.Collections.Generic;
using System.IO;
using DevAccelerationSystem.Core;
using UnityEditor;
using UnityEditor.Build.Player;

namespace DevAccelerationSystem.ProjectCompilationCheck
{
    [AssetPath(nameof(ProjectCompilationCheck) + "SO/" + nameof(ProjectCompilationConfigSO) + ".asset",
        AssetPathAttribute.Location.LocallyInParentFolderWithTheScriptType, typeof(ProjectCompilationConfigSO),
        nameof(DevAccelerationSystem))]
    internal sealed class ProjectCompilationConfigSO : SOSingleton<ProjectCompilationConfigSO>
    {
        public List<CompilationConfig> CompilationConfigs = new List<CompilationConfig>()
        {
            new CompilationConfig
            {
                Name = BuildTarget.Android.ToString(),
                Option = ScriptCompilationOptions.DevelopmentBuild,
                Target = BuildTarget.Android,
                Enabled = true,
                ExtraScriptingDefines = new[]
                    {"NOT_RELEASE", "JSONNET_XMLDISABLE", "USE_STRING_BUFFER", "NOT_UNITY_EDITOR"}
            }
        };
        
        public string DefaultCompilationOutputFileName = Path.Combine(Path.Combine("Library","ProjectCompilationCheckOutput"), "CompilationOutput.json");
    }
}