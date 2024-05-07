using System;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

namespace DevAccelerationSystem.ProjectCompilationCheck
{
    [Serializable]
    public class CompilationConfig //for extending and serialization ScriptCompilationSettings
    {
        [Tooltip("Please provide uniq name for the config to be able call it by name.")]
        public string Name;
        
        [Tooltip("If it is not enabled it will be skipped.")]
        public bool Enabled;
        
        [Tooltip("None - without DEVELOPMENT_BUILD and UNITY_ASSERTIONS, DevelopmentBuild will apply define symbol DEVELOPMENT_BUILD, Assertions - UNITY_ASSERTIONS")]
        public ScriptCompilationOptions Option;
        
        [Tooltip("Choose the target platform for the config. BuiltTargetGroup will be defined automatically.")]
        public BuildTarget Target;
        
        [Tooltip("Your custom scripting defines for the config.")]
        public string[] ExtraScriptingDefines;

        public override string ToString()
        {
            var d = ExtraScriptingDefines != null ? string.Join(",", ExtraScriptingDefines) : "[Empty]";
            return
                $"Name {Name} Enabled {Enabled} Option {Option} Target {Target} ExtraScriptingDefines {d}";
        }

        public static implicit operator ScriptCompilationSettings(CompilationConfig value)
        {
            return new ScriptCompilationSettings
            {
                group = value.Target.ConvertToBuildTargetGroup(), target = value.Target, options = value.Option,
                extraScriptingDefines = value.ExtraScriptingDefines
            };
        }
    }
}