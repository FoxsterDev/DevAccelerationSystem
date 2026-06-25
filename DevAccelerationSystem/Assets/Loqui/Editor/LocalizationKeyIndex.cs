using System;
using System.Collections.Generic;
using UnityEditor;

namespace Loqui.Editor
{
    /// <summary>
    /// Aggregates the localization keys/bool-keys declared across every <see cref="LocalizationCatalog"/>
    /// asset in the project. Shared by the key-dropdown drawer and the usage scanner. Cached; call
    /// <see cref="Refresh"/> after editing a catalog.
    /// </summary>
    public static class LocalizationKeyIndex
    {
        public readonly struct Entry
        {
            public readonly string Key;
            public readonly string Group;
            public readonly string English;
            public readonly string CatalogName;
            public readonly bool IsBool;

            public Entry(string key, string group, string english, string catalogName, bool isBool)
            {
                Key = key;
                Group = group;
                English = english;
                CatalogName = catalogName;
                IsBool = isBool;
            }
        }

        private static List<Entry> _entries;

        public static IReadOnlyList<Entry> Entries => _entries ??= Build();

        public static void Refresh()
        {
            _entries = Build();
        }

        public static bool ContainsTextKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            foreach (var entry in Entries)
            {
                if (!entry.IsBool && string.Equals(entry.Key, key, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<Entry> Build()
        {
            var list = new List<Entry>();
            var seenText = new HashSet<string>(StringComparer.Ordinal);
            var seenBool = new HashSet<string>(StringComparer.Ordinal);

            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(LocalizationCatalog)))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var catalog = AssetDatabase.LoadAssetAtPath<LocalizationCatalog>(path);
                if (catalog == null)
                {
                    continue;
                }

                if (catalog.Texts != null)
                {
                    foreach (var entry in catalog.Texts)
                    {
                        if (entry != null && !string.IsNullOrEmpty(entry.Key) && seenText.Add(entry.Key))
                        {
                            list.Add(new Entry(entry.Key, entry.Group, entry.EnglishFallback, catalog.name, false));
                        }
                    }
                }

                if (catalog.Bools != null)
                {
                    foreach (var entry in catalog.Bools)
                    {
                        if (entry != null && !string.IsNullOrEmpty(entry.Key) && seenBool.Add(entry.Key))
                        {
                            list.Add(new Entry(entry.Key, null, null, catalog.name, true));
                        }
                    }
                }
            }

            list.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            return list;
        }
    }
}
