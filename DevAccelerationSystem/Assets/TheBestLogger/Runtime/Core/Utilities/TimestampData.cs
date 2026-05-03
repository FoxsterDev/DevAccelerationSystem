using System;

namespace TheBestLogger.Core.Utilities
{
    internal sealed class CachedTimestampData
    {
        public readonly DateTime CachedUtc;
        public readonly string CachedFormattedString;

        public CachedTimestampData(DateTime cachedUtc, string cachedFormattedString)
        {
            CachedUtc = cachedUtc;
            CachedFormattedString = cachedFormattedString;
        }
    }
}
