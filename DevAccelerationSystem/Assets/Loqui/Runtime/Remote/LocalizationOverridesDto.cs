using System;

namespace Loqui.Remote
{
    [Serializable]
    public class LocalizationOverridesDto
    {
        public int SchemaVersion;
        public string PayloadVersion;
        public LocalizationOverrideLanguageDto[] Languages;
    }

    [Serializable]
    public class LocalizationOverrideLanguageDto
    {
        public string LanguageCode;
        public LocalizationOverrideEntryDto[] Entries;
    }

    [Serializable]
    public class LocalizationOverrideEntryDto
    {
        public string Key;
        public string Default;
        public string iOS;
        public string Android;
    }
}
