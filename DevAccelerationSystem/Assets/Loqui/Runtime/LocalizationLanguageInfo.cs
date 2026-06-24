namespace Loqui
{
    public readonly struct LocalizationLanguageInfo
    {
        public readonly string LanguageCode;
        public readonly string DisplayName;
        public readonly string NativeDisplayName;

        public LocalizationLanguageInfo(string languageCode, string displayName, string nativeDisplayName)
        {
            LanguageCode = languageCode;
            DisplayName = displayName;
            NativeDisplayName = nativeDisplayName;
        }
    }
}
