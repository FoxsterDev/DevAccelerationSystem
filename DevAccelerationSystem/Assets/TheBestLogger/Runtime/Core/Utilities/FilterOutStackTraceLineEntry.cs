namespace TheBestLogger.Core.Utilities
{
    [System.Serializable]
    public class FilterOutStackTraceLineEntry
    {
        public string DeclaringTypeNamespace = "";
        public FilterOutDeclaringTypeNameEntry[] TypeNameEntries = new FilterOutDeclaringTypeNameEntry[0];
    }
}
