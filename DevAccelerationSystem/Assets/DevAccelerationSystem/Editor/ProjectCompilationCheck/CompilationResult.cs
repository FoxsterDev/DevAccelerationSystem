using System;
using UnityEditor.Build.Player;
using UnityEngine;

namespace DevAccelerationSystem.ProjectCompilationCheck
{
    [Serializable]
    public class CompilationResult
    {
        /// <summary>
        /// Uniq name of the project compilation setting.
        /// </summary>
        public string ProjectCompilationSettingName;
        
        /// <summary>
        /// Technical insight about the specific compilation. Not it has only total time of compilation.
        /// </summary>
        public CompilationStats CompilationStats;
        
        /// <summary>
        /// Count of errors for the compilation.
        /// </summary>
        public uint ErrorsCount;
        
        /// <summary>
        /// Formatted errors list for the compilation.
        /// </summary>
        [TextArea(5, 50)] public string ErrorsList;
        
        /// <summary>
        /// It is an used config parameter for the compilation.
        /// </summary>
        public CompilationConfig CompilationConfig;
        
        /// <summary>
        /// Unity compilation result from an called Editor API.
        /// </summary>
        public ScriptCompilationResult UnityCompilationResult;
    }
}