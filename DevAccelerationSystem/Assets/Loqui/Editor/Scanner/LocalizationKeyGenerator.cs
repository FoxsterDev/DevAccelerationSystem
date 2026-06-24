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
                slug = HashSuffix(source);
            }

            if (string.IsNullOrEmpty(slug))
            {
                return groupSlug;
            }

            return string.IsNullOrEmpty(groupSlug) ? slug : groupSlug + "." + slug;
        }

        private static string HashSuffix(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var trimmed = text.Trim();
            if (trimmed.Length == 0)
            {
                return string.Empty;
            }

            const uint offset = 2166136261;
            const uint prime = 16777619;
            var hash = offset;
            for (var i = 0; i < trimmed.Length; i++)
            {
                hash ^= trimmed[i];
                hash *= prime;
            }

            return "x" + hash.ToString("x8", CultureInfo.InvariantCulture);
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
