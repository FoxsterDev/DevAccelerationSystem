using System;
using System.Collections.Generic;
using UnityEngine;

namespace Loqui
{
    [CreateAssetMenu(fileName = "LocalizationCatalog", menuName = "Loqui/Catalog")]
    public sealed class LocalizationCatalog : ScriptableObject
    {
        public int SchemaVersion = 1;
        public LocalizationLocaleSet Locales;
        public List<LocalizationTextTable> TextTables = new();

        public bool IsValid(out string error)
        {
            if (Locales == null)
            {
                error = "Catalog has no locale set.";
                return false;
            }

            if (!Locales.Validate(out error))
            {
                return false;
            }

            var seenKeys = new HashSet<string>(StringComparer.Ordinal);
            if (TextTables != null)
            {
                foreach (var table in TextTables)
                {
                    if (table == null)
                    {
                        continue;
                    }

                    if (!table.Validate(out error))
                    {
                        return false;
                    }

                    if (table.Entries == null)
                    {
                        continue;
                    }

                    foreach (var entry in table.Entries)
                    {
                        if (entry != null && !seenKeys.Add(entry.Key))
                        {
                            error = $"Catalog has a duplicate key '{entry.Key}' across text tables.";
                            return false;
                        }
                    }
                }
            }

            error = null;
            return true;
        }

        public bool TryGetLocale(string languageCode, out LocalizationLocaleProfile locale)
        {
            if (Locales != null)
            {
                return Locales.TryGetLocale(languageCode, out locale);
            }

            locale = null;
            return false;
        }

        public bool TryGetFontProfile(string languageCode, out LocalizationFontProfile fontProfile)
        {
            if (Locales != null)
            {
                return Locales.TryGetFontProfile(languageCode, out fontProfile);
            }

            fontProfile = null;
            return false;
        }

        public void CollectEnabledLanguageCodes(List<string> buffer)
        {
            Locales?.CollectEnabledLanguageCodes(buffer);
        }

        public bool TryGetEntry(string key, out LocalizationEntry entry)
        {
            if (TextTables != null && !string.IsNullOrEmpty(key))
            {
                foreach (var table in TextTables)
                {
                    if (table != null && table.TryGetEntry(key, out entry))
                    {
                        return true;
                    }
                }
            }

            entry = null;
            return false;
        }

        public void CollectEntries(List<LocalizationEntry> buffer)
        {
            if (buffer == null || TextTables == null)
            {
                return;
            }

            foreach (var table in TextTables)
            {
                if (table?.Entries == null)
                {
                    continue;
                }

                foreach (var entry in table.Entries)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.Key))
                    {
                        buffer.Add(entry);
                    }
                }
            }
        }
    }
}
