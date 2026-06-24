using System;
using System.Collections.Generic;

namespace Loqui
{
    public sealed class LocalizationActiveTable
    {
        private readonly Dictionary<string, string> _values;

        public string LanguageCode { get; }
        public int Count => _values.Count;

        private LocalizationActiveTable(string languageCode, Dictionary<string, string> values)
        {
            LanguageCode = languageCode;
            _values = values;
        }

        public bool TryGet(string key, out string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                value = null;
                return false;
            }

            return _values.TryGetValue(key, out value);
        }

        public static LocalizationActiveTable Build(
            LocalizationCatalog catalog,
            string languageCode,
            LocalizationPlatform platform,
            List<LocalizationEntry> entryBuffer)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            if (catalog != null && entryBuffer != null)
            {
                entryBuffer.Clear();
                catalog.CollectEntries(entryBuffer);
                for (var i = 0; i < entryBuffer.Count; i++)
                {
                    var entry = entryBuffer[i];
                    if (LocalizationValueResolver.TryResolve(entry, languageCode, platform, out var value))
                    {
                        values[entry.Key] = value;
                    }
                }
            }

            return new LocalizationActiveTable(languageCode, values);
        }
    }
}
