namespace Loqui.Remote
{
    public sealed class LocalizationOverridesResult
    {
        public bool Accepted;
        public string RejectionReason;
        public int SchemaVersion;
        public string PayloadVersion;
        public int LanguageCount;
        public int KeyCount;
        public int RejectedCount;
        public LocalizationOverridesDto Payload;

        public static LocalizationOverridesResult Reject(string reason)
        {
            return new LocalizationOverridesResult
            {
                Accepted = false,
                RejectionReason = reason
            };
        }
    }
}
