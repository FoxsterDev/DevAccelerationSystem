using System;

namespace Loqui
{
    [Serializable]
    public class LocalizationLocaleProfile
    {
        [LocalizationLanguageCode] public string LanguageCode;
        public string DisplayName;
        public string NativeDisplayName;
        public string CultureName;
        public LocalizationFontProfile FontProfile = new();
        public bool Enabled = true;

        public LocalizationLanguageInfo ToLanguageInfo()
        {
            return new LocalizationLanguageInfo(LanguageCode, DisplayName, NativeDisplayName);
        }
    }
}
