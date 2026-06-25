using System;
using System.Collections.Generic;

namespace Loqui
{
    public static class LocalizationLanguageCodes
    {
        // Top world languages (mobile-localization oriented). English is the required hard fallback.
        public const string English = "en";
        public const string ChineseSimplified = "zh-Hans";
        public const string ChineseTraditional = "zh-Hant";
        public const string Hindi = "hi";
        public const string Spanish = "es";
        public const string Arabic = "ar";
        public const string French = "fr";
        public const string Russian = "ru";
        public const string Portuguese = "pt";
        public const string BrazilianPortuguese = "pt-BR";
        public const string Indonesian = "id";
        public const string German = "de";
        public const string Japanese = "ja";
        public const string Korean = "ko";
        public const string Turkish = "tr";
        public const string Italian = "it";
        public const string Vietnamese = "vi";

        public static readonly string[] All =
        {
            English,
            ChineseSimplified,
            ChineseTraditional,
            Hindi,
            Spanish,
            Arabic,
            French,
            Russian,
            Portuguese,
            BrazilianPortuguese,
            Indonesian,
            German,
            Japanese,
            Korean,
            Turkish,
            Italian,
            Vietnamese
        };

        private static readonly Dictionary<string, string> DisplayNames = new(StringComparer.OrdinalIgnoreCase)
        {
            { English, "English" },
            { ChineseSimplified, "Chinese (Simplified)" },
            { ChineseTraditional, "Chinese (Traditional)" },
            { Hindi, "Hindi" },
            { Spanish, "Spanish" },
            { Arabic, "Arabic" },
            { French, "French" },
            { Russian, "Russian" },
            { Portuguese, "Portuguese" },
            { BrazilianPortuguese, "Portuguese (Brazil)" },
            { Indonesian, "Indonesian" },
            { German, "German" },
            { Japanese, "Japanese" },
            { Korean, "Korean" },
            { Turkish, "Turkish" },
            { Italian, "Italian" },
            { Vietnamese, "Vietnamese" }
        };

        public static bool IsEnglish(string code)
        {
            return Equals(code, English);
        }

        public static bool IsKnown(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return false;
            }

            for (var i = 0; i < All.Length; i++)
            {
                if (Equals(All[i], code))
                {
                    return true;
                }
            }

            return false;
        }

        public static string DisplayName(string code)
        {
            return !string.IsNullOrEmpty(code) && DisplayNames.TryGetValue(code, out var name) ? name : code;
        }

        public static string DisplayLabel(string code)
        {
            return DisplayNames.TryGetValue(code ?? string.Empty, out var name) ? $"{name} ({code})" : code;
        }

        public static bool Equals(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
