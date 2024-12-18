namespace TheBestLogger.Core.Utilities
{
    [System.Serializable]
    internal class StackTraceFormatterConfiguration
    {
        public bool NeedFileInfo = false;
        public int MaximumInnerExceptionDepth = 3;
        public bool Utf16ValueStringBuilder = false;
        public int SkipFrames = 5;

        public FilterOutStackTraceLineEntry[] FilterOutLinesWhen = new[]
        {
            new FilterOutStackTraceLineEntry { DeclaringTypeNamespace = "UnityEngine", DeclaringTypeName = "Debug" },
            new FilterOutStackTraceLineEntry { DeclaringTypeNamespace = "UnityEngine", DeclaringTypeName = "Logger" },
            new FilterOutStackTraceLineEntry { DeclaringTypeNamespace = "UnityEngine", DeclaringTypeName = "DebugLogHandler" },
            new FilterOutStackTraceLineEntry { DeclaringTypeNamespace = "UnityEngine", DeclaringTypeName = "StackTraceUtility" },
            new FilterOutStackTraceLineEntry { DeclaringTypeNamespace = "TheBestLogger", DeclaringTypeName = "" },
            new FilterOutStackTraceLineEntry { DeclaringTypeNamespace = "UnityEngine.Assertions", DeclaringTypeName = "Assert" },
            new FilterOutStackTraceLineEntry { DeclaringTypeNamespace = "UnityEngine", DeclaringTypeName = "MonoBehaviour", MethodName = "print"},
            new FilterOutStackTraceLineEntry { DeclaringTypeNamespace = "UnityEngine", DeclaringTypeName = "UnitySynchronizationContext"},
            new FilterOutStackTraceLineEntry { DeclaringTypeNamespace = "System.Runtime.CompilerServices", DeclaringTypeName = "MoveNextRunner"},
            new FilterOutStackTraceLineEntry { DeclaringTypeNamespace = "System.Threading", DeclaringTypeName = "ExecutionContext"},
        };
    }
}
