namespace TheBestLogger.Core.Utilities
{
    [System.Serializable]
    internal class FilterOutStackTraceLineEntry
    {
        public string DeclaringTypeNamespace = "";
        public FilterOutDeclaringTypeNameEntry[] TypeNameEntries = new FilterOutDeclaringTypeNameEntry[0];
    }
}
