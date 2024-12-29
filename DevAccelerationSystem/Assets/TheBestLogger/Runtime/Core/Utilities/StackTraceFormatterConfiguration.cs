namespace TheBestLogger.Core.Utilities
{
    [System.Serializable]
    internal class StackTraceFormatterConfiguration
    {
        public bool Enabled = true;
        public bool NeedFileInfo = false;
        public int MaximumInnerExceptionDepth = 3;
        public bool Utf16ValueStringBuilder = false;
        public int SkipFrames = 5;

        public FilterOutStackTraceLineEntry[] FilterOutLinesWhen = new[]
        {
            new FilterOutStackTraceLineEntry
            {
                DeclaringTypeNamespace = "UnityEngine", TypeNameEntries = new FilterOutDeclaringTypeNameEntry[]
                {
                    new FilterOutDeclaringTypeNameEntry { DeclaringTypeName = "Debug" },
                    new FilterOutDeclaringTypeNameEntry { DeclaringTypeName = "Logger" },
                    new FilterOutDeclaringTypeNameEntry { DeclaringTypeName = "DebugLogHandler" },
                    new FilterOutDeclaringTypeNameEntry { DeclaringTypeName = "StackTraceUtility" },
                    new FilterOutDeclaringTypeNameEntry { DeclaringTypeName = "MonoBehaviour", MethodName = "print" },
                    new FilterOutDeclaringTypeNameEntry { DeclaringTypeName = "UnitySynchronizationContext" },
                    new FilterOutDeclaringTypeNameEntry { DeclaringTypeName = "WorkRequest" },
                }
            },
            new FilterOutStackTraceLineEntry
            {
                DeclaringTypeNamespace = "TheBestLogger", TypeNameEntries = new FilterOutDeclaringTypeNameEntry[]
                {
                    new FilterOutDeclaringTypeNameEntry { DeclaringTypeName = "" },
                }
            },
            new FilterOutStackTraceLineEntry
            {
                DeclaringTypeNamespace = "UnityEngine.Assertions", TypeNameEntries = new FilterOutDeclaringTypeNameEntry[]
                {
                    new FilterOutDeclaringTypeNameEntry { DeclaringTypeName = "Assert" },
                }
            },
            new FilterOutStackTraceLineEntry
            {
                DeclaringTypeNamespace = "System.Runtime.CompilerServices", TypeNameEntries = new FilterOutDeclaringTypeNameEntry[]
                {
                    new FilterOutDeclaringTypeNameEntry { DeclaringTypeName = "MoveNextRunner" },
                }
            },
            new FilterOutStackTraceLineEntry
            {
                DeclaringTypeNamespace = "System.Threading", TypeNameEntries = new FilterOutDeclaringTypeNameEntry[]
                {
                    new FilterOutDeclaringTypeNameEntry { DeclaringTypeName = "ExecutionContext" },
                }
            }
        };
    }
}
