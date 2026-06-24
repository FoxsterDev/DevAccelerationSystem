using System;

namespace Loqui
{
    [Serializable]
    public class LocalizationSettingsScope
    {
        public bool EnabledByDefault = true;
        public LocalizationCatalog Catalog;
        [LocalizationLanguageCode] public string DefaultLanguageCode = LocalizationLanguageCodes.English;
    }
}
