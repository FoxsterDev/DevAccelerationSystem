using System;
using System.Collections.Generic;

namespace Loqui.Editor
{
    [Serializable]
    public sealed class LocalizationScanInventory
    {
        public int Count;
        public int CandidateCount;
        public List<LocalizationScanItem> Items = new();
    }

    [Serializable]
    public sealed class LocalizationAiBundleEntry
    {
        public string Key;
        public string Group;
        public string Source;
        public string Context;
        public int MaxLength;
        public string RecommendedApproach;
        public string MutationEvidence;
        public string TargetLanguage;
        public string TargetDefault;
        public string TargetIOS;
        public string TargetAndroid;
    }

    [Serializable]
    public sealed class LocalizationAiBundle
    {
        public int SchemaVersion = 1;
        public string SourceLanguage = LocalizationLanguageCodes.English;
        public string TargetLanguage;
        public int Count;
        public List<LocalizationAiBundleEntry> Entries = new();
    }
}
