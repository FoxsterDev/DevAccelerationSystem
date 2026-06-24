using System;

namespace Loqui
{
    public static class LocalizationLanguageCodes
    {
        public const string English = "en";
        public const string BrazilianPortuguese = "pt-BR";

        public static readonly string[] All = { English, BrazilianPortuguese };

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

        public static bool Equals(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
