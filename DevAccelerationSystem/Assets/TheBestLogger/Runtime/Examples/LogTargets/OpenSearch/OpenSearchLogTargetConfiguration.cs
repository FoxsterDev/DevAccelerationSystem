namespace TheBestLogger.Examples.LogTargets
{
    [System.Serializable]
    public sealed class OpenSearchLogTargetConfiguration : LogTargetConfiguration
    {
        public string OpenSearchHostUrl;
        public string OpenSearchSingleLogMethod;
        public string IndexPrefix;
        public string ApiKey;
    }
}