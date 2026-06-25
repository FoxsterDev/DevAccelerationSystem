using System;
using System.Collections.Generic;
using UnityEngine;

namespace Loqui
{
    [Serializable]
    public class LocalizationEntry
    {
        [Tooltip("Stable, unique key, e.g. 'home.play_now'. Never renamed once shipped; unique across the whole catalog.")]
        public string Key;

        [Tooltip("English source string. Returned verbatim as the fallback when no override resolves — never leave it empty.")]
        [TextArea]
        public string EnglishFallback;

        [Tooltip("Per-language values. Each has a Default plus optional iOS / Android overrides; the 'en' value mirrors EnglishFallback.")]
        public List<LocalizationLanguageValue> Languages = new();

        [Tooltip("Surface slug to organize entries and match per-feature key classes, e.g. 'home', 'tutorial'.")]
        public string Group;

        [Tooltip("Max visible characters before the UI clips/overflows. 0 = no limit. A translator hint, not enforced at runtime.")]
        public int MaxLength;

        [Tooltip("Author/translator-facing: where and how the text appears, plus placeholder semantics (e.g. {0} = coin amount). Not shipped at runtime.")]
        [TextArea]
        public string Context;

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
