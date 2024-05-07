using System;
using System.Collections.Generic;

namespace DevAccelerationSystem.ProjectCompilationCheck
{
    [Serializable]
    public class CompilationOutput
    {
        /// <summary>
        /// List of compilation results for each project compilation setting.
        /// </summary>
        public List<CompilationResult> Results = new List<CompilationResult>(1);
        
        /// <summary>
        /// It will provide technical insights on the compilation process. Now it has only total time of compilation.
        /// </summary>
        public CompilationStats Stats;
        
        /// <summary>
        /// All specific tool's logs from the compilation process. It can be useful for debugging.
        /// </summary>
        public string Logs;
    }
}