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

        internal void Set(string key, string value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _values[key] = value;
            }
        }

        public static LocalizationActiveTable Build(
            LocalizationCatalog catalog,
            string languageCode,
            LocalizationPlatform platform,
            List<LocalizationEntry> entryBuffer)
        {
            Dictionary<string, string> values;
            if (catalog != null && entryBuffer != null)
            {
                entryBuffer.Clear();
                catalog.CollectEntries(entryBuffer);
                values = new Dictionary<string, string>(entryBuffer.Count, StringComparer.Ordinal);
                for (var i = 0; i < entryBuffer.Count; i++)
                {
                    var entry = entryBuffer[i];
                    if (LocalizationValueResolver.TryResolve(entry, languageCode, platform, out var value))
                    {
                        values[entry.Key] = value;
                    }
                }
            }
            else
            {
                values = new Dictionary<string, string>(StringComparer.Ordinal);
            }

            return new LocalizationActiveTable(languageCode, values);
        }
    }
}
