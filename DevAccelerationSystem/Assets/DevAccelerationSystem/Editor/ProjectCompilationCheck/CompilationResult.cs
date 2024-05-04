using System;
using UnityEditor.Build.Player;
using UnityEngine;

namespace DevAccelerationSystem.ProjectCompilationCheck
{
    [Serializable]
    public class CompilationResult
    {
        public string ProjectCompilationSettingName;
        public CompilationStats CompilationStats;
        public uint ErrorsCount;
        [TextArea(5, 50)] public string ErrorsList;
        
        public CompilationConfig CompilationConfig;
        public ScriptCompilationResult UnityCompilationResult;
    }
}