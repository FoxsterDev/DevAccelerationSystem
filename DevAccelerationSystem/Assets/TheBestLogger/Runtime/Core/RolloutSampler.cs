namespace TheBestLogger
{
    internal static class RolloutSampler
    {
        private const ulong FNV_OFFSET_BASIS = 14695981039346656037UL;
        private const ulong FNV_PRIME = 1099511628211UL;
        private const ulong BUCKET_COUNT = 10000UL;

        internal static bool IsRolloutActive(float rolloutPercentage)
        {
            return rolloutPercentage > 0f && rolloutPercentage < 100f;
        }

        internal static bool ShouldEnable(string sessionKey,
                                          int configurationApplyVersion,
                                          int categoryIndex,
                                          string categoryName,
                                          float rolloutPercentage)
        {
            if (!IsRolloutActive(rolloutPercentage))
            {
                return true;
            }

            return ComputeBucketPercentage(sessionKey, configurationApplyVersion, categoryIndex, categoryName) < rolloutPercentage;
        }

        internal static float ComputeBucketPercentage(string sessionKey,
                                                      int configurationApplyVersion,
                                                      int categoryIndex,
                                                      string categoryName)
        {
            ulong hash = FNV_OFFSET_BASIS;
            hash = AddString(hash, sessionKey);
            hash = AddInt(hash, configurationApplyVersion);
            hash = AddInt(hash, categoryIndex);
            hash = AddString(hash, categoryName);
            return (hash % BUCKET_COUNT) / 100f;
        }

        private static ulong AddInt(ulong hash, int value)
        {
            unchecked
            {
                hash ^= (uint) value;
                hash *= FNV_PRIME;
                return hash;
            }
        }

        private static ulong AddString(ulong hash, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return AddInt(hash, 0);
            }

            unchecked
            {
                for (var index = 0; index < value.Length; index++)
                {
                    hash ^= value[index];
                    hash *= FNV_PRIME;
                }

                return hash;
            }
        }
    }
}
