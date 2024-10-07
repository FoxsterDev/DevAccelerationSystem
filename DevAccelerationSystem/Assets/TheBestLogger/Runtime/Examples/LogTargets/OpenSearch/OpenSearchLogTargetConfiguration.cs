namespace TheBestLogger.Examples.LogTargets
{
    [System.Serializable]
    public sealed class OpenSearchLogTargetConfiguration : LogTargetConfiguration
    {
        public string OpenSearchHostUrl;
        public string OpenSearchSingleLogMethod;
        public string IndexPrefix;
        public string ApiKey;

        public override void Merge(LogTargetConfiguration newConfig)
        {
            base.Merge(newConfig);

            if (newConfig is OpenSearchLogTargetConfiguration newOpenSearchConfig)
            {
                if (!string.IsNullOrEmpty(newOpenSearchConfig.OpenSearchHostUrl))
                    OpenSearchHostUrl = newOpenSearchConfig.OpenSearchHostUrl;
                if (!string.IsNullOrEmpty(newOpenSearchConfig.OpenSearchSingleLogMethod))
                    OpenSearchSingleLogMethod = newOpenSearchConfig.OpenSearchSingleLogMethod;
                if (!string.IsNullOrEmpty(newOpenSearchConfig.IndexPrefix))
                    IndexPrefix = newOpenSearchConfig.IndexPrefix;
                if (!string.IsNullOrEmpty(newOpenSearchConfig.ApiKey))
                    ApiKey = newOpenSearchConfig.ApiKey;
            }
        }
    }
}