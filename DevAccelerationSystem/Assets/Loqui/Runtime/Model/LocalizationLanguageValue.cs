using System;

namespace Loqui
{
    [Serializable]
    public class LocalizationLanguageValue
    {
        [LocalizationLanguageCode] public string LanguageCode;
        public LocalizationPlatformValues Values = new();
    }
}
