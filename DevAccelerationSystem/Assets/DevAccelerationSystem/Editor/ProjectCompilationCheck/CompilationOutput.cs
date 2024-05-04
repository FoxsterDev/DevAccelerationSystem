using System;
using System.Collections.Generic;

namespace DevAccelerationSystem.ProjectCompilationCheck
{
    [Serializable]
    public class CompilationOutput
    {
        public List<CompilationResult> Results = new List<CompilationResult>(1);
        public CompilationStats Stats;
        public string Logs;
    }
}