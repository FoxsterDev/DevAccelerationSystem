using System;
using System.Collections.Generic;
using UnityEngine;

namespace Loqui
{
    [Serializable]
    public class LocalizationEntry
    {
        public string Key;
        [TextArea] public string EnglishFallback;
        public List<LocalizationLanguageValue> Languages = new();
        public string Group;
        public int MaxLength;
        [TextArea] public string Context;
        [TextArea] public string Notes;

        public bool TryGetLanguageValues(string languageCode, out LocalizationPlatformValues values)
        {
            if (Languages != null)
            {
                for (var i = 0; i < Languages.Count; i++)
                {
                    var entry = Languages[i];
                    if (entry != null && LocalizationLanguageCodes.Equals(entry.LanguageCode, languageCode))
                    {
                        values = entry.Values;
                        return values != null;
                    }
                }
            }

            values = null;
            return false;
        }
    }
}
