using System.Globalization;
using System.Text;

namespace Loqui.Editor
{
    public static class LocalizationKeyGenerator
    {
        public const int DefaultMaxWords = 6;

        public static string Generate(string group, string source, int maxWords = DefaultMaxWords)
        {
            var slug = Slug(source, maxWords);
            var groupSlug = Slug(group, int.MaxValue);
            if (string.IsNullOrEmpty(slug))
            {
                return groupSlug;
            }

            return string.IsNullOrEmpty(groupSlug) ? slug : groupSlug + "." + slug;
        }

        public static string Slug(string text, int maxWords)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var normalized = text.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            var pendingSeparator = false;
            var words = 0;

            for (var i = 0; i < normalized.Length; i++)
            {
                var c = normalized[i];
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(c) && c < 128)
                {
                    if (pendingSeparator && builder.Length > 0)
                    {
                        if (words >= maxWords)
                        {
                            break;
                        }

                        builder.Append('_');
                    }

                    if (builder.Length == 0 || pendingSeparator)
                    {
                        words++;
                    }

                    builder.Append(char.ToLowerInvariant(c));
                    pendingSeparator = false;
                }
                else
                {
                    pendingSeparator = builder.Length > 0;
                }
            }

            return builder.ToString();
        }
    }
}
