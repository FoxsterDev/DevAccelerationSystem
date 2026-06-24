using System;

namespace Loqui.Editor
{
    public enum LocalizationScanSource
    {
        TmpText,
        LegacyText,
        SerializedString,
        CSharpLiteral
    }

    public static class LocalizationRecommendedApproaches
    {
        public const string ComponentAttach = "ComponentAttach";
        public const string CodeApi = "CodeApi";
        public const string Conflict = "Conflict";
        public const string Exclude = "Exclude";
    }

    [Serializable]
    public sealed class LocalizationScanItem
    {
        public LocalizationScanSource Source;
        public string AssetPath;
        public int LineNumber;
        public string ContainerName;
        public string ContainerKind;
        public string HierarchyPath;
        public string ComponentType;
        public string TextComponentId;
        public string EnglishSource;
        public string ProposedKey;
        public string Group;
        public int MaxLength;
        public string PlatformDefault;
        public string PlatformIOS;
        public string PlatformAndroid;
        public string ExclusionReason;
        public string Context;
        public string Notes;
        public string RecommendedApproach;
        public string CodeMutatorHint;
        public string MutationEvidence;
        public bool RequiresReview;
        public bool IsCandidate;

        public bool IsExcluded => !string.IsNullOrEmpty(ExclusionReason);
    }
}
